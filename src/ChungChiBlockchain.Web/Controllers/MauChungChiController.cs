using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.ToChucCap)]
public class MauChungChiController : ToChucControllerBase
{
    private readonly IMauChungChiService _svc;
    private readonly INhatKyService _nhatKy;

    public MauChungChiController(IMauChungChiService svc, INhatKyService nhatKy)
    {
        _svc = svc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync(ToChucHienTai));

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new MauChungChiFormViewModel();
        await NapLoaiAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MauChungChiFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapLoaiAsync(vm);
            return View(vm);
        }

        var kq = await _svc.TaoAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            await NapLoaiAsync(vm);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Tạo mẫu chứng chỉ", "mau_chung_chi", vm.TenMau.Trim());
        TempData["ThongBao"] = $"Đã tạo mẫu \"{vm.TenMau.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(ToChucHienTai, id);
        if (vm is null) return NotFound();
        await NapLoaiAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MauChungChiFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapLoaiAsync(vm);
            return View(vm);
        }

        var kq = await _svc.CapNhatAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            await NapLoaiAsync(vm);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa mẫu chứng chỉ", "mau_chung_chi", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật mẫu chứng chỉ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, bool hoatDong)
    {
        var kq = await _svc.DoiTrangThaiAsync(ToChucHienTai, id, hoatDong);
        if (kq.ThanhCong)
        {
            await _nhatKy.GhiAsync(User.LayId(), hoatDong ? "Bật mẫu chứng chỉ" : "Tắt mẫu chứng chỉ", "mau_chung_chi", id.ToString());
            TempData["ThongBao"] = hoatDong ? "Đã bật mẫu." : "Đã tắt mẫu (không dùng để cấp mới).";
        }
        else
        {
            TempData["Loi"] = kq.Loi;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task NapLoaiAsync(MauChungChiFormViewModel vm)
    {
        var ds = await _svc.DanhSachLoaiAsync();
        vm.DanhSachLoai = ds.Select(x => new SelectListItem(x.Value, x.Key.ToString(), x.Key == vm.LoaiChungChiId));
    }
}
