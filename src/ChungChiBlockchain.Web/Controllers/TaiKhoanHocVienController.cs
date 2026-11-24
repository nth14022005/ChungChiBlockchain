using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.QuanTri)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TaiKhoanHocVienController(AppDbContext db, INhatKyService nhatKy) : Controller
{
    [HttpGet]
    public Task<IActionResult> Create() => FormAsync(new CapTaiKhoanHocVien());

    private async Task<IActionResult> FormAsync(CapTaiKhoanHocVien form)
    {
        form.HoSo = await db.NguoiHocs.AsNoTracking()
            .Where(s => !db.TaiKhoanNguoiHocs.Any(link => link.NguoiHocId == s.Id))
            .OrderBy(s => s.ToChuc.TenToChuc).ThenBy(s => s.MaNguoiHoc)
            .Select(s => new SelectListItem {
                Value = s.Id.ToString(),
                Text = s.ToChuc.TenToChuc + " · " + s.MaNguoiHoc + " · " + s.HoTen
            }).ToListAsync();
        return View("Create", form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CapTaiKhoanHocVien form)
    {
        if (!ModelState.IsValid) return await FormAsync(form);
        var profile = await db.NguoiHocs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == form.NguoiHocId);
        if (profile is null || await db.TaiKhoanNguoiHocs.AnyAsync(x => x.NguoiHocId == profile.Id))
        {
            ModelState.AddModelError(nameof(form.NguoiHocId), "Hồ sơ không tồn tại hoặc đã có tài khoản.");
            return await FormAsync(form);
        }
        var name = form.TenDangNhap.Trim();
        if (await db.NguoiDungs.AnyAsync(x => x.TenDangNhap.ToLower() == name.ToLower()))
        {
            ModelState.AddModelError(nameof(form.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
            return await FormAsync(form);
        }
        var role = await db.VaiTros.SingleAsync(x => x.MaVaiTro == VaiTroHocVien.Ma);
        var user = new NguoiDung {
            TenDangNhap = name, HoTen = profile.HoTen, Email = profile.Email,
            ToChucId = profile.ToChucId, VaiTroId = role.Id, TrangThai = true, NgayTao = DateTime.Now
        };
        var password = MatKhauTienIch.SinhMatKhauTam();
        user.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(user, password);
        db.TaiKhoanNguoiHocs.Add(new TaiKhoanNguoiHoc { NguoiHocId = profile.Id, NguoiDung = user });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            ModelState.AddModelError("", "Không lưu được tài khoản. Kiểm tra tên đăng nhập và hồ sơ đã chọn.");
            return await FormAsync(form);
        }
        await nhatKy.GhiAsync(User.LayId(), "Cấp tài khoản người học", "nguoi_dung", user.Id.ToString());
        return View("~/Views/NguoiDung/MatKhauTamThoi.cshtml", new MatKhauTamThoiViewModel {
            TieuDe = "Đã cấp tài khoản người học", TenDangNhap = name, HoTen = profile.HoTen, MatKhau = password
        });
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "NguoiDung");
}
