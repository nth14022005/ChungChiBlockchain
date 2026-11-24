using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.QuanTri)]
public class ToChucController : Controller
{
    private readonly IToChucService _svc;
    private readonly INhatKyService _nhatKy;

    public ToChucController(IToChucService svc, INhatKyService nhatKy)
    {
        _svc = svc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync());

    [HttpGet]
    public IActionResult Create() => View(new ToChucFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ToChucFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.TaoAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Tạo tổ chức", "to_chuc", vm.MaToChuc.Trim().ToUpperInvariant());
        TempData["ThongBao"] = $"Đã tạo tổ chức \"{vm.TenToChuc.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ToChucFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.CapNhatAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa tổ chức", "to_chuc", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật thông tin tổ chức.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, string trangThai)
    {
        var kq = await _svc.DoiTrangThaiAsync(id, trangThai);
        if (kq.ThanhCong)
        {
            await _nhatKy.GhiAsync(User.LayId(), "Đổi trạng thái tổ chức", "to_chuc", $"{id} -> {trangThai}");
            TempData["ThongBao"] = $"Đã chuyển trạng thái sang \"{TrangThaiToChuc.Nhan(trangThai)}\".";
        }
        else
        {
            TempData["Loi"] = kq.Loi;
        }
        return RedirectToAction(nameof(Index));
    }
}
