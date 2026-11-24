using System.Security.Claims;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChungChiBlockchain.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly INhatKyService _nhatKy;

    public AccountController(IAuthService auth, INhatKyService nhatKy)
    {
        _auth = auth;
        _nhatKy = nhatKy;
    }

    // ------------------------------------------------------------ Đăng nhập
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (!await _auth.CoNguoiDungAsync())
            return RedirectToAction(nameof(Setup));
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.MatKhau = string.Empty;
            return View(model);
        }

        var kq = await _auth.DangNhapAsync(model.TenDangNhap, model.MatKhau);
        if (!kq.ThanhCong || kq.NguoiDung is null)
        {
            await _nhatKy.GhiAsync(null, "Đăng nhập thất bại", "nguoi_dung", model.TenDangNhap);
            ModelState.AddModelError(string.Empty, kq.Loi ?? "Sai tên đăng nhập hoặc mật khẩu.");
            model.MatKhau = string.Empty;
            return View(model);
        }

        await DangNhapBangCookieAsync(kq.NguoiDung);
        await _nhatKy.GhiAsync(kq.NguoiDung.Id, "Đăng nhập", "nguoi_dung", kq.NguoiDung.TenDangNhap);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        if (kq.NguoiDung.MaVaiTro == ChungChiBlockchain.Web.Models.VaiTroHocVien.Ma)
            return RedirectToAction("Index", "HocVien");
        return RedirectToAction("Index", "Home");
    }

    // ------------------------------------------------------------ Đăng xuất
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var id = User.LayId();
        var ten = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _nhatKy.GhiAsync(id, "Đăng xuất", "nguoi_dung", ten);
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------ Đổi mật khẩu
    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new DoiMatKhauViewModel());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(DoiMatKhauViewModel model)
    {
        if (!ModelState.IsValid) return View(new DoiMatKhauViewModel());

        var id = User.LayId() ?? 0;
        var kq = await _auth.DoiMatKhauAsync(id, model.MatKhauHienTai, model.MatKhauMoi);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(new DoiMatKhauViewModel());
        }

        await _nhatKy.GhiAsync(id, "Đổi mật khẩu", "nguoi_dung", User.Identity?.Name);
        TempData["ThongBao"] = "Đã đổi mật khẩu thành công.";
        return RedirectToAction("Index", "Home");
    }

    // -------------------------------------- Khởi tạo quản trị đầu tiên (chạy 1 lần)
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Setup()
    {
        if (await _auth.CoNguoiDungAsync())
            return RedirectToAction(nameof(Login));
        return View(new SetupViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(SetupViewModel model)
    {
        if (await _auth.CoNguoiDungAsync())
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }

        try
        {
            var nd = await _auth.TaoQuanTriDauTienAsync(model.TenDangNhap, model.MatKhau, model.HoTen, model.Email);
            await DangNhapBangCookieAsync(nd);
            await _nhatKy.GhiAsync(nd.Id, "Khởi tạo quản trị đầu tiên", "nguoi_dung", nd.TenDangNhap);
            return RedirectToAction("Index", "Admin");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // --------------------------------------------------------------- nội bộ
    private async Task DangNhapBangCookieAsync(NguoiDungDangNhap nd)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nd.Id.ToString()),
            new(ClaimTypes.Name, nd.TenDangNhap),
            new(ClaimTypes.Role, nd.MaVaiTro),
            new("ho_ten", nd.HoTen),
            new("ten_vai_tro", nd.TenVaiTro)
        };
        if (nd.ToChucId is not null)
            claims.Add(new Claim("to_chuc_id", nd.ToChucId.Value.ToString()));

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
