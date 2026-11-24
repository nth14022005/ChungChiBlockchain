using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroHocVien.Ma)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HocVienController(AppDbContext db, ICertificateFileStorage files, IHashService hash) : Controller
{
    private IQueryable<ChungChi> ChungChiCuaToi()
    {
        var userId = User.LayId();
        return db.ChungChis.AsNoTracking().Where(c =>
            (c.TrangThai == "da_ghi_chuoi" || c.TrangThai == "thu_hoi") &&
            db.TaiKhoanNguoiHocs.Any(link => link.NguoiDungId == userId &&
                link.NguoiHocId == c.NguoiHocId && link.NguoiDung.TrangThai &&
                link.NguoiDung.VaiTro.MaVaiTro == VaiTroHocVien.Ma));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await ChungChiCuaToi().OrderByDescending(c => c.NgayCap).ThenByDescending(c => c.Id)
            .Select(c => new ChungChiCaNhan(c.Id, c.MaChungChi, c.KhoaHoc.TenKhoaHoc,
                c.NgayCap, c.XepLoai, c.TrangThai)).ToListAsync(ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> TaiPdf(int id, CancellationToken ct)
    {
        var file = await (from c in ChungChiCuaToi()
                          join t in db.TepChungChis on c.Id equals t.ChungChiId
                          where c.Id == id
                          select new { t.DuongDan, c.MaChungChi, c.HashChungChi })
                         .FirstOrDefaultAsync(ct);
        if (file is null) return NotFound();
        var path = files.GetAbsolutePath(file.DuongDan);
        if (!System.IO.File.Exists(path)) return NotFound("Không tìm thấy tệp PDF.");
        var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
        if (hash.BamNoiDung(bytes) != file.HashChungChi) return NotFound("Tệp PDF lưu trữ không khớp bản phát hành.");
        return File(bytes, "application/pdf", file.MaChungChi + ".pdf");
    }
}
