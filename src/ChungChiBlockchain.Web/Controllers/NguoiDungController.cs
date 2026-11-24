using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.QuanTri)]
public class NguoiDungController : Controller
{
    private readonly INguoiDungService _svc;
    private readonly IToChucService _toChuc;
    private readonly INhatKyService _nhatKy;

    public NguoiDungController(INguoiDungService svc, IToChucService toChuc, INhatKyService nhatKy)
    {
        _svc = svc;
        _toChuc = toChuc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new NguoiDungTaoViewModel();
        await NapToChucAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create(NguoiDungTaoViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapToChucAsync(vm);
            return View(vm);
        }

        var kq = await _svc.TaoAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            await NapToChucAsync(vm);
            return View(vm);
        }

        // KHÔNG ghi mật khẩu vào nhật ký
        await _nhatKy.GhiAsync(User.LayId(), "Tạo người dùng", "nguoi_dung", kq.TenDangNhap);
        return View("MatKhauTamThoi", new MatKhauTamThoiViewModel
        {
            TieuDe = "Đã tạo tài khoản",
            TenDangNhap = kq.TenDangNhap!, HoTen = kq.HoTen!, MatKhau = kq.MatKhauTam!
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(id);
        if (vm is null) return NotFound();
        await NapToChucAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NguoiDungSuaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapLaiThongTinAsync(vm);
            return View(vm);
        }

        var kq = await _svc.CapNhatAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            await NapLaiThongTinAsync(vm);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa người dùng", "nguoi_dung", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật thông tin người dùng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, bool hoatDong)
    {
        var kq = await _svc.DoiTrangThaiAsync(id, hoatDong, User.LayId() ?? 0);
        if (kq.ThanhCong)
        {
            await _nhatKy.GhiAsync(User.LayId(), hoatDong ? "Mở khóa người dùng" : "Khóa người dùng", "nguoi_dung", id.ToString());
            TempData["ThongBao"] = hoatDong ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản. Có hiệu lực ngay cả với phiên đang đăng nhập.";
        }
        else
        {
            TempData["Loi"] = kq.Loi;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DatLaiMatKhau(int id)
    {
        var kq = await _svc.DatLaiMatKhauAsync(id, User.LayId() ?? 0);
        if (!kq.ThanhCong)
        {
            TempData["Loi"] = kq.Loi;
            return RedirectToAction(nameof(Index));
        }

        await _nhatKy.GhiAsync(User.LayId(), "Đặt lại mật khẩu người dùng", "nguoi_dung", kq.TenDangNhap);
        return View("MatKhauTamThoi", new MatKhauTamThoiViewModel
        {
            TieuDe = "Đã đặt lại mật khẩu",
            TenDangNhap = kq.TenDangNhap!, HoTen = kq.HoTen!, MatKhau = kq.MatKhauTam!
        });
    }

    private async Task NapToChucAsync(NguoiDungTaoViewModel vm)
    {
        var ds = await _toChuc.DanhSachChonAsync();
        vm.DanhSachToChuc = ds.Select(x => new SelectListItem(x.Value, x.Key.ToString(), x.Key == vm.ToChucId));
    }

    private async Task NapToChucAsync(NguoiDungSuaViewModel vm)
    {
        var ds = await _toChuc.DanhSachChonAsync();
        vm.DanhSachToChuc = ds.Select(x => new SelectListItem(x.Value, x.Key.ToString(), x.Key == vm.ToChucId));
    }

    /// <summary>Khi form sửa không hợp lệ: nạp lại các trường chỉ-đọc (không được gửi lên từ form).</summary>
    private async Task NapLaiThongTinAsync(NguoiDungSuaViewModel vm)
    {
        var goc = await _svc.LayFormAsync(vm.Id);
        if (goc is not null)
        {
            vm.TenDangNhap = goc.TenDangNhap;
            vm.VaiTro = goc.VaiTro;
            vm.TenVaiTro = goc.TenVaiTro;
        }
        await NapToChucAsync(vm);
    }
}
