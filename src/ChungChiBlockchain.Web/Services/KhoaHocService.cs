using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public class KhoaHocItem
{
    public int Id { get; set; }
    public string MaKhoaHoc { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string? ThoiLuong { get; set; }
    public string? MoTa { get; set; }
}

/// <summary>Mọi hàm đều nhận toChucId lấy từ phiên đăng nhập: nhân viên chỉ thấy và sửa dữ liệu của tổ chức mình.</summary>
public interface IKhoaHocService
{
    Task<List<KhoaHocItem>> LietKeAsync(int toChucId);
    Task<KhoaHocFormViewModel?> LayFormAsync(int toChucId, int id);
    Task<KetQuaThaoTac> TaoAsync(int toChucId, KhoaHocFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(int toChucId, KhoaHocFormViewModel vm);
    Task<KetQuaThaoTac> XoaAsync(int toChucId, int id);
}

public class KhoaHocService : IKhoaHocService
{
    private readonly AppDbContext _db;

    public KhoaHocService(AppDbContext db) => _db = db;

    public Task<List<KhoaHocItem>> LietKeAsync(int toChucId) =>
        _db.KhoaHocs.AsNoTracking().Where(x => x.ToChucId == toChucId).OrderBy(x => x.MaKhoaHoc)
            .Select(x => new KhoaHocItem
            {
                Id = x.Id, MaKhoaHoc = x.MaKhoaHoc, TenKhoaHoc = x.TenKhoaHoc, ThoiLuong = x.ThoiLuong, MoTa = x.MoTa
            }).ToListAsync();

    public Task<KhoaHocFormViewModel?> LayFormAsync(int toChucId, int id) =>
        _db.KhoaHocs.AsNoTracking().Where(x => x.Id == id && x.ToChucId == toChucId)
            .Select(x => new KhoaHocFormViewModel
            {
                Id = x.Id, MaKhoaHoc = x.MaKhoaHoc, TenKhoaHoc = x.TenKhoaHoc, ThoiLuong = x.ThoiLuong, MoTa = x.MoTa
            }).FirstOrDefaultAsync();

    public async Task<KetQuaThaoTac> TaoAsync(int toChucId, KhoaHocFormViewModel vm)
    {
        var ma = vm.MaKhoaHoc.Trim().ToUpperInvariant();
        if (await _db.KhoaHocs.AnyAsync(x => x.ToChucId == toChucId && x.MaKhoaHoc == ma))
            return KetQuaThaoTac.Loi_($"Mã khóa học '{ma}' đã tồn tại trong tổ chức của bạn.");

        _db.KhoaHocs.Add(new KhoaHoc
        {
            ToChucId = toChucId,
            MaKhoaHoc = ma,
            TenKhoaHoc = vm.TenKhoaHoc.Trim(),
            ThoiLuong = Rong(vm.ThoiLuong),
            MoTa = Rong(vm.MoTa)
        });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(int toChucId, KhoaHocFormViewModel vm)
    {
        var k = await _db.KhoaHocs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.ToChucId == toChucId);
        if (k is null) return KetQuaThaoTac.Loi_("Không tìm thấy khóa học.");

        // Mã khóa học không đổi (có thể đã được dùng trong chứng chỉ)
        k.TenKhoaHoc = vm.TenKhoaHoc.Trim();
        k.ThoiLuong = Rong(vm.ThoiLuong);
        k.MoTa = Rong(vm.MoTa);
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> XoaAsync(int toChucId, int id)
    {
        var k = await _db.KhoaHocs.FirstOrDefaultAsync(x => x.Id == id && x.ToChucId == toChucId);
        if (k is null) return KetQuaThaoTac.Loi_("Không tìm thấy khóa học.");
        if (await _db.ChungChis.AnyAsync(c => c.KhoaHocId == id))
            return KetQuaThaoTac.Loi_("Không xóa được: khóa học này đã có chứng chỉ được cấp.");

        _db.KhoaHocs.Remove(k);
        return await LuuAsync();
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
            return KetQuaThaoTac.Loi_("Không lưu được (có thể bị trùng dữ liệu hoặc đang được sử dụng).");
        }
    }

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
