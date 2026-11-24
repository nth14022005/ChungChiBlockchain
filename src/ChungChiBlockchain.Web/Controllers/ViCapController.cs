using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Controllers;

/// <summary>Quản lý ví blockchain được phép cấp chứng chỉ của từng tổ chức.</summary>
[Authorize(Roles = VaiTroMa.QuanTri)]
public class ViCapController : Controller
{
    private readonly IViCapService _svc;
    private readonly IToChucService _toChuc;
    private readonly INhatKyService _nhatKy;

    public ViCapController(IViCapService svc, IToChucService toChuc, INhatKyService nhatKy)
    {
        _svc = svc;
        _toChuc = toChuc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index() => View(await _svc.LietKeAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ViCapFormViewModel();
        await NapDanhSachAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ViCapFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapDanhSachAsync(vm);
            return View(vm);
        }

        var kq = await _svc.TaoAsync(vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            await NapDanhSachAsync(vm);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Thêm ví cấp chứng chỉ", "vi_blockchain", vm.DiaChiVi.Trim().ToLowerInvariant());
        TempData["ThongBao"] = "Đã thêm ví. Ví đang ở trạng thái \"Chờ duyệt\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string? nhan)
    {
        var kq = await _svc.CapNhatNhanAsync(id, nhan);
        if (!kq.ThanhCong)
        {
            TempData["Loi"] = kq.Loi;
            return RedirectToAction(nameof(Index));
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa nhãn ví", "vi_blockchain", id.ToString());
        TempData["ThongBao"] = "Đã cập nhật nhãn ví.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, string trangThai)
    {
        var kq = await _svc.DoiTrangThaiAsync(id, trangThai);
        if (kq.ThanhCong)
        {
            await _nhatKy.GhiAsync(User.LayId(), "Đổi trạng thái ví", "vi_blockchain", $"{id} -> {trangThai}");
            TempData["ThongBao"] = $"Đã chuyển trạng thái ví sang \"{TrangThaiVi.Nhan(trangThai)}\".";
        }
        else
        {
            TempData["Loi"] = kq.Loi;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task NapDanhSachAsync(ViCapFormViewModel vm)
    {
        var ds = await _toChuc.DanhSachChonAsync();
        vm.DanhSachToChuc = ds.Select(x => new SelectListItem(x.Value, x.Key.ToString(), x.Key == vm.ToChucId));
    }
}
