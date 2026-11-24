using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChungChiBlockchain.Web.Services;

public class ToChucItem
{
    public int Id { get; set; }
    public string MaToChuc { get; set; } = string.Empty;
    public string TenToChuc { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? EmailLienHe { get; set; }
    public string? Website { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public int SoNhanVien { get; set; }
    public int SoVi { get; set; }
    public DateTime NgayTao { get; set; }
}

public interface IToChucService
{
    Task<List<ToChucItem>> LietKeAsync();
    Task<ToChucFormViewModel?> LayFormAsync(int id);
    Task<KetQuaThaoTac> TaoAsync(ToChucFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(ToChucFormViewModel vm);
    Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, string trangThai);
    Task<List<KeyValuePair<int, string>>> DanhSachChonAsync();
}

public class ToChucService : IToChucService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public ToChucService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<ToChucItem>> LietKeAsync()
    {
        var ds = await _db.ToChucs.AsNoTracking().OrderBy(x => x.TenToChuc).ToListAsync();
        var nv = await _db.NguoiDungs.Where(u => u.ToChucId != null)
            .GroupBy(u => u.ToChucId).Select(g => new { Id = g.Key, N = g.Count() }).ToListAsync();
        var vi = await _db.ViBlockchains
            .GroupBy(v => v.ToChucId).Select(g => new { Id = g.Key, N = g.Count() }).ToListAsync();

        var dNv = nv.Where(x => x.Id != null).ToDictionary(x => x.Id!.Value, x => x.N);
        var dVi = vi.ToDictionary(x => x.Id, x => x.N);

        return ds.Select(t => new ToChucItem
        {
            Id = t.Id, MaToChuc = t.MaToChuc, TenToChuc = t.TenToChuc, DiaChi = t.DiaChi,
            EmailLienHe = t.EmailLienHe, Website = t.Website, TrangThai = t.TrangThai, NgayTao = t.NgayTao,
            SoNhanVien = dNv.GetValueOrDefault(t.Id), SoVi = dVi.GetValueOrDefault(t.Id)
        }).ToList();
    }

    public async Task<ToChucFormViewModel?> LayFormAsync(int id)
    {
        var t = await _db.ToChucs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? null : new ToChucFormViewModel
        {
            Id = t.Id, MaToChuc = t.MaToChuc, TenToChuc = t.TenToChuc, DiaChi = t.DiaChi,
            EmailLienHe = t.EmailLienHe, Website = t.Website, TrangThai = t.TrangThai
        };
    }

    public async Task<KetQuaThaoTac> TaoAsync(ToChucFormViewModel vm)
    {
        var ma = vm.MaToChuc.Trim().ToUpperInvariant();
        if (!TrangThaiToChuc.TatCa.Contains(vm.TrangThai))
            return KetQuaThaoTac.Loi_("Trạng thái không hợp lệ.");
        if (await _db.ToChucs.AnyAsync(x => x.MaToChuc == ma))
            return KetQuaThaoTac.Loi_($"Mã tổ chức '{ma}' đã tồn tại.");

        _db.ToChucs.Add(new ToChuc
        {
            MaToChuc = ma,
            TenToChuc = vm.TenToChuc.Trim(),
            DiaChi = Rong(vm.DiaChi),
            EmailLienHe = Rong(vm.EmailLienHe),
            Website = Rong(vm.Website),
            TrangThai = vm.TrangThai,
            NgayTao = DateTime.Now
        });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(ToChucFormViewModel vm)
    {
        var t = await _db.ToChucs.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (t is null) return KetQuaThaoTac.Loi_("Không tìm thấy tổ chức.");

        // Mã tổ chức KHÔNG được đổi: nó nằm trong mã của mọi chứng chỉ đã cấp.
        t.TenToChuc = vm.TenToChuc.Trim();
        t.DiaChi = Rong(vm.DiaChi);
        t.EmailLienHe = Rong(vm.EmailLienHe);
        t.Website = Rong(vm.Website);
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, string trangThai)
    {
        if (!TrangThaiToChuc.TatCa.Contains(trangThai))
            return KetQuaThaoTac.Loi_("Trạng thái không hợp lệ.");
        var t = await _db.ToChucs.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return KetQuaThaoTac.Loi_("Không tìm thấy tổ chức.");

        t.TrangThai = trangThai;
        var kq = await LuuAsync();
        if (kq.ThanhCong)
        {
            // Nhân viên của tổ chức bị ảnh hưởng ngay (không chờ hết hạn bộ nhớ đệm)
            var ids = await _db.NguoiDungs.Where(u => u.ToChucId == id).Select(u => u.Id).ToListAsync();
            TaiKhoanBoNho.XoaNhieu(_cache, ids);
        }
        return kq;
    }

    public async Task<List<KeyValuePair<int, string>>> DanhSachChonAsync()
    {
        var ds = await _db.ToChucs.AsNoTracking().OrderBy(x => x.TenToChuc)
            .Select(x => new { x.Id, Ten = x.MaToChuc + " - " + x.TenToChuc }).ToListAsync();
        return ds.Select(x => new KeyValuePair<int, string>(x.Id, x.Ten)).ToList();
    }

    private async Task<KetQuaThaoTac> LuuAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return KetQuaThaoTac.Ok();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return KetQuaThaoTac.Loi_("Không lưu được (có thể bị trùng dữ liệu hoặc quá dài). Vui lòng kiểm tra lại.");
        }
    }

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
