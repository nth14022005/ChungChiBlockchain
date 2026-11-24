using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChungChiBlockchain.Web.Controllers;

/// <summary>Danh mục loại chứng chỉ dùng chung, chỉ quản trị viên được thêm và sửa.</summary>
[Authorize(Roles = VaiTroMa.QuanTri)]
public class LoaiChungChiController : Controller
{
    private readonly ILoaiChungChiService _svc;
    private readonly INhatKyService _nhatKy;

    public LoaiChungChiController(ILoaiChungChiService svc, INhatKyService nhatKy)
    {
        _svc = svc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync());

    [HttpGet]
    public IActionResult Create() => View(new LoaiChungChiFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoaiChungChiFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.TaoAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Tạo loại chứng chỉ", "loai_chung_chi", vm.TenLoai.Trim());
        TempData["ThongBao"] = $"Đã tạo loại \"{vm.TenLoai.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LoaiChungChiFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.CapNhatAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa loại chứng chỉ", "loai_chung_chi", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật loại chứng chỉ.";
        return RedirectToAction(nameof(Index));
    }
}
