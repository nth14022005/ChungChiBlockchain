using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public class LoaiChungChiItem
{
    public int Id { get; set; }
    public string TenLoai { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}

/// <summary>Danh mục loại chứng chỉ dùng chung toàn hệ thống, do quản trị viên quản lý.</summary>
public interface ILoaiChungChiService
{
    Task<List<LoaiChungChiItem>> LietKeAsync();
    Task<LoaiChungChiFormViewModel?> LayFormAsync(int id);
    Task<KetQuaThaoTac> TaoAsync(LoaiChungChiFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(LoaiChungChiFormViewModel vm);
}

public class LoaiChungChiService : ILoaiChungChiService
{
    private readonly AppDbContext _db;

    public LoaiChungChiService(AppDbContext db) => _db = db;

    public Task<List<LoaiChungChiItem>> LietKeAsync() =>
        _db.LoaiChungChis.AsNoTracking().OrderBy(x => x.TenLoai)
            .Select(x => new LoaiChungChiItem { Id = x.Id, TenLoai = x.TenLoai, MoTa = x.MoTa }).ToListAsync();

    public Task<LoaiChungChiFormViewModel?> LayFormAsync(int id) =>
        _db.LoaiChungChis.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new LoaiChungChiFormViewModel { Id = x.Id, TenLoai = x.TenLoai, MoTa = x.MoTa })
            .FirstOrDefaultAsync();

    public async Task<KetQuaThaoTac> TaoAsync(LoaiChungChiFormViewModel vm)
    {
        var ten = vm.TenLoai.Trim();
        var tenThuong = ten.ToLower();
        if (await _db.LoaiChungChis.AnyAsync(x => x.TenLoai.ToLower() == tenThuong))
            return KetQuaThaoTac.Loi_($"Loại chứng chỉ '{ten}' đã tồn tại.");

        _db.LoaiChungChis.Add(new LoaiChungChi { TenLoai = ten, MoTa = Rong(vm.MoTa) });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(LoaiChungChiFormViewModel vm)
    {
        var l = await _db.LoaiChungChis.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (l is null) return KetQuaThaoTac.Loi_("Không tìm thấy loại chứng chỉ.");

        var ten = vm.TenLoai.Trim();
        var tenThuong = ten.ToLower();
        if (await _db.LoaiChungChis.AnyAsync(x => x.Id != vm.Id && x.TenLoai.ToLower() == tenThuong))
            return KetQuaThaoTac.Loi_($"Loại chứng chỉ '{ten}' đã tồn tại.");

        l.TenLoai = ten;
        l.MoTa = Rong(vm.MoTa);
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
            return KetQuaThaoTac.Loi_("Không lưu được (có thể bị trùng tên).");
        }
    }

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
