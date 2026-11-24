using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.ToChucCap)]
public class KhoaHocController : ToChucControllerBase
{
    private readonly IKhoaHocService _svc;
    private readonly INhatKyService _nhatKy;

    public KhoaHocController(IKhoaHocService svc, INhatKyService nhatKy)
    {
        _svc = svc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync(ToChucHienTai));

    [HttpGet]
    public IActionResult Create() => View(new KhoaHocFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhoaHocFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.TaoAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Tạo khóa học", "khoa_hoc", vm.MaKhoaHoc.Trim().ToUpperInvariant());
        TempData["ThongBao"] = $"Đã tạo khóa học \"{vm.TenKhoaHoc.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(ToChucHienTai, id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KhoaHocFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.CapNhatAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa khóa học", "khoa_hoc", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật khóa học.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var kq = await _svc.XoaAsync(ToChucHienTai, id);
        if (kq.ThanhCong)
        {
            await _nhatKy.GhiAsync(User.LayId(), "Xóa khóa học", "khoa_hoc", id.ToString());
            TempData["ThongBao"] = "Đã xóa khóa học.";
        }
        else
        {
            TempData["Loi"] = kq.Loi;
        }
        return RedirectToAction(nameof(Index));
    }
}
