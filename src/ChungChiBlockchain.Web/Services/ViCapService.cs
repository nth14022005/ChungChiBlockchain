using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public class ViCapItem
{
    public int Id { get; set; }
    public int ToChucId { get; set; }
    public string TenToChuc { get; set; } = string.Empty;
    public string DiaChiVi { get; set; } = string.Empty;
    public string? Nhan { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayThem { get; set; }
}

public interface IViCapService
{
    Task<List<ViCapItem>> LietKeAsync();
    Task<ViCapFormViewModel?> LayFormAsync(int id);
    Task<KetQuaThaoTac> TaoAsync(ViCapFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatNhanAsync(int id, string? nhan);
    Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, string trangThai);
}

public class ViCapService : IViCapService
{
    private readonly AppDbContext _db;

    public ViCapService(AppDbContext db) => _db = db;

    public async Task<List<ViCapItem>> LietKeAsync()
    {
        var vi = await _db.ViBlockchains.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var tc = await _db.ToChucs.AsNoTracking().Select(t => new { t.Id, t.TenToChuc }).ToListAsync();
        var ten = tc.ToDictionary(t => t.Id, t => t.TenToChuc);

        return vi.Select(v => new ViCapItem
        {
            Id = v.Id, ToChucId = v.ToChucId, TenToChuc = ten.GetValueOrDefault(v.ToChucId, "?"),
            DiaChiVi = v.DiaChiVi, Nhan = v.Nhan, TrangThai = v.TrangThai, NgayThem = v.NgayThem
        }).ToList();
    }

    public async Task<ViCapFormViewModel?> LayFormAsync(int id)
    {
        var v = await _db.ViBlockchains.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? null : new ViCapFormViewModel
        {
            Id = v.Id, ToChucId = v.ToChucId, DiaChiVi = v.DiaChiVi, Nhan = v.Nhan
        };
    }

    public async Task<KetQuaThaoTac> TaoAsync(ViCapFormViewModel vm)
    {
        if (vm.ToChucId is null || !await _db.ToChucs.AnyAsync(t => t.Id == vm.ToChucId))
            return KetQuaThaoTac.Loi_("Tổ chức không tồn tại.");

        // Địa chỉ ví không phân biệt hoa thường: chuẩn hóa chữ thường để chống trùng
        var diaChi = vm.DiaChiVi.Trim().ToLowerInvariant();
        if (await _db.ViBlockchains.AnyAsync(v => v.DiaChiVi == diaChi))
            return KetQuaThaoTac.Loi_("Địa chỉ ví này đã được đăng ký.");

        _db.ViBlockchains.Add(new ViBlockchain
        {
            ToChucId = vm.ToChucId.Value,
            DiaChiVi = diaChi,
            Nhan = string.IsNullOrWhiteSpace(vm.Nhan) ? null : vm.Nhan.Trim(),
            TrangThai = TrangThaiVi.ChoDuyet,
            NgayThem = DateTime.Now
        });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatNhanAsync(int id, string? nhan)
    {
        var v = await _db.ViBlockchains.FirstOrDefaultAsync(x => x.Id == id);
        if (v is null) return KetQuaThaoTac.Loi_("Không tìm thấy ví.");
        v.Nhan = string.IsNullOrWhiteSpace(nhan) ? null : nhan.Trim();   // địa chỉ ví không được sửa
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, string trangThai)
    {
        if (!TrangThaiVi.TatCa.Contains(trangThai))
            return KetQuaThaoTac.Loi_("Trạng thái không hợp lệ.");
        var v = await _db.ViBlockchains.FirstOrDefaultAsync(x => x.Id == id);
        if (v is null) return KetQuaThaoTac.Loi_("Không tìm thấy ví.");
        v.TrangThai = trangThai;
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
            return KetQuaThaoTac.Loi_("Không lưu được (có thể bị trùng dữ liệu). Vui lòng kiểm tra lại.");
        }
    }
}
