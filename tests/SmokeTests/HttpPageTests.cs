using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using ChungChiBlockchain.Web.Controllers;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Blockchain;
using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public static class HttpPageTests
{
    public static async Task Run(string root,DbContextOptions<AppDbContext> options,TestnetOptions testnet,int certificateId)
    {
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ApplicationName=typeof(BlockchainController).Assembly.GetName().Name,ContentRootPath=root,EnvironmentName="Development"});
        builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews();builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped(_=>new AppDbContext(options));
        builder.Services.AddSingleton(Options.Create(testnet));builder.Services.AddHttpClient<EthereumRpcService>();
        builder.Services.AddScoped<IBlockchainService>(sp=>sp.GetRequiredService<EthereumRpcService>());
        builder.Services.AddSingleton<DemoOperationLock>();builder.Services.AddScoped<TestnetWorkflow>();
        builder.Services.AddScoped<IChungChiService,ChungChiService>();builder.Services.AddSingleton<IHashService,HashServiceV1>();
        builder.Services.AddSingleton<ICertificatePdfService,CertificatePdfService>();builder.Services.Configure<FileStorageOptions>(_=>{});builder.Services.AddSingleton<ICertificateFileStorage,CertificateFileStorage>();
        builder.Services.AddScoped<INhatKyService,NhatKyService>();
        builder.Services.AddAuthentication("TestOnly").AddScheme<AuthenticationSchemeOptions,TestAuthentication>("TestOnly",_=>{});builder.Services.AddAuthorization();
        await using var app=builder.Build();app.UseRouting();app.UseAuthentication();app.UseAuthorization();app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");await app.StartAsync();
        using var client=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){BaseAddress=new Uri(app.Urls.Single())};
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception("FAIL HTTP: "+text);Console.WriteLine("PASS HTTP: "+text);count++;}
        client.DefaultRequestHeaders.Add("X-Test-Identity","admin");
        var admin=await client.GetAsync("/Blockchain/Vi");var html=await admin.Content.ReadAsStringAsync();Check(admin.StatusCode==HttpStatusCode.OK&&html.Contains("wallet-admin"),"Admin without organization can render issuer page");
        var cfg=await client.GetAsync("/Blockchain/Config");var json=await cfg.Content.ReadAsStringAsync();Check(cfg.IsSuccessStatusCode&&!json.Contains("rpcUrl",StringComparison.OrdinalIgnoreCase),"Public wallet config excludes RPC credentials");
        client.DefaultRequestHeaders.Remove("X-Test-Identity");client.DefaultRequestHeaders.Add("X-Test-Identity","staff");
        Check((await client.GetAsync("/Blockchain/Vi")).StatusCode==HttpStatusCode.Forbidden,"Staff cannot open issuer administration");
        var page=await client.GetAsync($"/Blockchain/ChungChi/{certificateId}");var body=await page.Content.ReadAsStringAsync();Check(page.IsSuccessStatusCode&&body.Contains("metamask.js")&&body.Contains("__RequestVerificationToken"),"Certificate Razor renders JS and anti-forgery token");
        var post=await client.PostAsync("/Blockchain/Prepare",new StringContent("{}",Encoding.UTF8,"application/json"));Check(post.StatusCode==HttpStatusCode.BadRequest,"Mutation without anti-forgery token rejected");
        client.DefaultRequestHeaders.Remove("X-Test-Identity");client.DefaultRequestHeaders.Add("X-Test-Identity","other");
        Check((await client.GetAsync($"/Blockchain/ChungChi/{certificateId}")).StatusCode==HttpStatusCode.NotFound,"Other organization cannot read certificate page");
        client.DefaultRequestHeaders.Remove("X-Test-Identity");
        Check((await client.GetAsync($"/Blockchain/ChungChi/{certificateId}")).StatusCode==HttpStatusCode.Unauthorized,"Anonymous cannot open issuance page");
        await app.StopAsync();Console.WriteLine($"HTTP checks: {count}");
    }
    // This synthetic authentication exists only in the tests project, never in the web application.
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var who=Request.Headers["X-Test-Identity"].ToString();if(who=="")return Task.FromResult(AuthenticateResult.NoResult());
            var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,"1"),new(ClaimTypes.Name,who),new(ClaimTypes.Role,who=="admin"?VaiTroMa.QuanTri:VaiTroMa.ToChucCap)};
            if(who!="admin")claims.Add(new("to_chuc_id",who=="other"?"2":"1"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,"TestOnly")),"TestOnly")));
        }
    }
}
