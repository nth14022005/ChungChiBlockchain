using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Auth;
using ChungChiBlockchain.Web.Services.Blockchain;
using ChungChiBlockchain.Web.Services.Hash;
using ChungChiBlockchain.Web.Services.ChungChi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// ----- CSDL -----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ----- Đăng nhập bằng cookie + phân quyền theo vai trò -----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ChungChi.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Mỗi request: tài khoản còn hoạt động và còn thuộc đúng tổ chức đã ghi trong phiên đăng nhập không?
        // Khóa tài khoản, tạm khóa tổ chức hay chuyển tổ chức đều có hiệu lực ngay (buộc đăng nhập lại).
        options.Events.OnValidatePrincipal = async context =>
        {
            var ok = int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            if (ok)
            {
                int? toChucTrongPhien = int.TryParse(context.Principal?.FindFirstValue("to_chuc_id"), out var tc) ? tc : null;
                var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var st = await auth.LayTrangThaiAsync(id);
                ok = st.HoatDong && st.ToChucId == toChucTrongPhien;
            }
            if (!ok)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode=429;
    options.AddPolicy("verify", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0 }));
});
builder.Services.AddSingleton<DemoOperationLock>();
builder.Services.AddScoped<PhatHanhGiaLapService>();
builder.Services.AddHostedService<GiaoDichGiaLapWorker>();
builder.Services.Configure<TestnetOptions>(builder.Configuration.GetSection("Testnet"));
builder.Services.AddHttpClient<EthereumRpcService>(client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<ChungChiBlockchain.Web.Services.CertificateReader>(client => client.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<TestnetWorkflow>();
builder.Services.AddHostedService<TestnetWorker>();

// ----- Dịch vụ nghiệp vụ -----
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INhatKyService, NhatKyService>();
builder.Services.AddScoped<IToChucService, ToChucService>();
builder.Services.AddScoped<IViCapService, ViCapService>();
builder.Services.AddScoped<INguoiDungService, NguoiDungService>();
builder.Services.AddScoped<IKhoaHocService, KhoaHocService>();
builder.Services.AddScoped<ILoaiChungChiService, LoaiChungChiService>();
builder.Services.AddScoped<IMauChungChiService, MauChungChiService>();
builder.Services.AddScoped<INguoiHocService, NguoiHocService>();
builder.Services.AddSingleton<IHashService, HashServiceV1>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddSingleton<ICertificateFileStorage, CertificateFileStorage>();
builder.Services.AddSingleton<ICertificatePdfService, CertificatePdfService>();
builder.Services.AddScoped<IChungChiService, ChungChiService>();

// ----- Đọc cả dữ liệu giả lập cũ; chế độ cấu hình quyết định nơi cấp mới -----
builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection("Blockchain"));
builder.Services.AddSingleton<GiaLapBlockchainService>();
var cheDoChuoi = builder.Configuration["Blockchain:Mode"] ?? "GiaLap";
if (cheDoChuoi.Equals("GiaLap", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IBlockchainService>(sp => sp.GetRequiredService<GiaLapBlockchainService>());
}
else if (cheDoChuoi.Equals("Testnet", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IBlockchainService>(sp => sp.GetRequiredService<EthereumRpcService>());
}
else
{
    throw new InvalidOperationException(
        $"Chế độ blockchain '{cheDoChuoi}' chưa được hỗ trợ ở phiên bản này. Dùng \"GiaLap\".");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();   // phải đứng trước UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
