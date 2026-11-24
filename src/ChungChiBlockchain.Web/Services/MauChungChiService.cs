using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public class MauChungChiItem
{
    public int Id { get; set; }
    public string TenMau { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public bool TrangThai { get; set; }
}

public interface IMauChungChiService
{
    Task<List<MauChungChiItem>> LietKeAsync(int toChucId);
    Task<MauChungChiFormViewModel?> LayFormAsync(int toChucId, int id);
    Task<KetQuaThaoTac> TaoAsync(int toChucId, MauChungChiFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(int toChucId, MauChungChiFormViewModel vm);
    Task<KetQuaThaoTac> DoiTrangThaiAsync(int toChucId, int id, bool hoatDong);
    Task<List<KeyValuePair<int, string>>> DanhSachLoaiAsync();
}

/// <summary>Mẫu chứng chỉ của từng tổ chức. Tệp nền/bố cục mẫu sẽ được bổ sung ở bước tạo PDF (cột duong_dan_mau).</summary>
public class MauChungChiService : IMauChungChiService
{
    private readonly AppDbContext _db;

    public MauChungChiService(AppDbContext db) => _db = db;

    public async Task<List<MauChungChiItem>> LietKeAsync(int toChucId)
    {
        var mau = await _db.MauChungChis.AsNoTracking().Where(x => x.ToChucId == toChucId).OrderBy(x => x.Id).ToListAsync();
        var loai = await _db.LoaiChungChis.AsNoTracking().Select(l => new { l.Id, l.TenLoai }).ToListAsync();
        var ten = loai.ToDictionary(l => l.Id, l => l.TenLoai);

        return mau.Select(m => new MauChungChiItem
        {
            Id = m.Id, TenMau = m.TenMau, TenLoai = ten.GetValueOrDefault(m.LoaiChungChiId, "?"), TrangThai = m.TrangThai
        }).ToList();
    }

    public Task<MauChungChiFormViewModel?> LayFormAsync(int toChucId, int id) =>
        _db.MauChungChis.AsNoTracking().Where(x => x.Id == id && x.ToChucId == toChucId)
            .Select(x => new MauChungChiFormViewModel { Id = x.Id, TenMau = x.TenMau, LoaiChungChiId = x.LoaiChungChiId })
            .FirstOrDefaultAsync();

    public async Task<KetQuaThaoTac> TaoAsync(int toChucId, MauChungChiFormViewModel vm)
    {
        if (vm.LoaiChungChiId is null || !await _db.LoaiChungChis.AnyAsync(l => l.Id == vm.LoaiChungChiId))
            return KetQuaThaoTac.Loi_("Loại chứng chỉ không tồn tại.");

        _db.MauChungChis.Add(new MauChungChi
        {
            ToChucId = toChucId,
            LoaiChungChiId = vm.LoaiChungChiId.Value,
            TenMau = vm.TenMau.Trim(),
            TrangThai = true
        });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(int toChucId, MauChungChiFormViewModel vm)
    {
        var m = await _db.MauChungChis.FirstOrDefaultAsync(x => x.Id == vm.Id && x.ToChucId == toChucId);
        if (m is null) return KetQuaThaoTac.Loi_("Không tìm thấy mẫu chứng chỉ.");
        if (vm.LoaiChungChiId is null || !await _db.LoaiChungChis.AnyAsync(l => l.Id == vm.LoaiChungChiId))
            return KetQuaThaoTac.Loi_("Loại chứng chỉ không tồn tại.");

        m.TenMau = vm.TenMau.Trim();
        m.LoaiChungChiId = vm.LoaiChungChiId.Value;
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> DoiTrangThaiAsync(int toChucId, int id, bool hoatDong)
    {
        var m = await _db.MauChungChis.FirstOrDefaultAsync(x => x.Id == id && x.ToChucId == toChucId);
        if (m is null) return KetQuaThaoTac.Loi_("Không tìm thấy mẫu chứng chỉ.");
        m.TrangThai = hoatDong;   // mẫu đã dùng cấp chứng chỉ thì chỉ ngừng dùng, không xóa
        return await LuuAsync();
    }

    public async Task<List<KeyValuePair<int, string>>> DanhSachLoaiAsync()
    {
        var ds = await _db.LoaiChungChis.AsNoTracking().OrderBy(x => x.TenLoai)
            .Select(x => new { x.Id, x.TenLoai }).ToListAsync();
        return ds.Select(x => new KeyValuePair<int, string>(x.Id, x.TenLoai)).ToList();
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
            return KetQuaThaoTac.Loi_("Không lưu được. Vui lòng kiểm tra lại.");
        }
    }
}
