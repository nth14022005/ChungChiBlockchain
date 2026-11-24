using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChungChiBlockchain.Web.Services;

public class NguoiDungItem
{
    public int Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MaVaiTro { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
    public int? ToChucId { get; set; }
    public string? TenToChuc { get; set; }
    public bool TrangThai { get; set; }
    public DateTime NgayTao { get; set; }
}

public interface INguoiDungService
{
    Task<List<NguoiDungItem>> LietKeAsync();
    Task<NguoiDungSuaViewModel?> LayFormAsync(int id);
    Task<KetQuaTaoNguoiDung> TaoAsync(NguoiDungTaoViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(NguoiDungSuaViewModel vm);
    Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, bool hoatDong, int nguoiThucHienId);
    Task<KetQuaTaoNguoiDung> DatLaiMatKhauAsync(int id, int nguoiThucHienId);
}

public class NguoiDungService : INguoiDungService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public NguoiDungService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<List<NguoiDungItem>> LietKeAsync() =>
        _db.NguoiDungs.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new NguoiDungItem
            {
                Id = x.Id, TenDangNhap = x.TenDangNhap, HoTen = x.HoTen, Email = x.Email,
                MaVaiTro = x.VaiTro.MaVaiTro, TenVaiTro = x.VaiTro.TenVaiTro,
                ToChucId = x.ToChucId, TenToChuc = x.ToChuc != null ? x.ToChuc.TenToChuc : null,
                TrangThai = x.TrangThai, NgayTao = x.NgayTao
            }).ToListAsync();

    public async Task<NguoiDungSuaViewModel?> LayFormAsync(int id)
    {
        var u = await _db.NguoiDungs.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new NguoiDungSuaViewModel
            {
                Id = x.Id, TenDangNhap = x.TenDangNhap, HoTen = x.HoTen, Email = x.Email,
                VaiTro = x.VaiTro.MaVaiTro, TenVaiTro = x.VaiTro.TenVaiTro, ToChucId = x.ToChucId
            }).FirstOrDefaultAsync();
        return u;
    }

    public async Task<KetQuaTaoNguoiDung> TaoAsync(NguoiDungTaoViewModel vm)
    {
        var ten = vm.TenDangNhap.Trim();
        if (vm.VaiTro != VaiTroMa.QuanTri && vm.VaiTro != VaiTroMa.ToChucCap)
            return Loi("Vai trò không hợp lệ.");

        var vaiTro = await _db.VaiTros.FirstOrDefaultAsync(v => v.MaVaiTro == vm.VaiTro);
        if (vaiTro is null) return Loi("Chưa có vai trò này trong CSDL. Hãy chạy lại script SQL.");

        int? toChucId = null;
        if (vm.VaiTro == VaiTroMa.ToChucCap)
        {
            if (vm.ToChucId is null) return Loi("Nhân viên tổ chức cấp phải thuộc một tổ chức.");
            if (!await _db.ToChucs.AnyAsync(t => t.Id == vm.ToChucId)) return Loi("Tổ chức không tồn tại.");
            toChucId = vm.ToChucId;
        }

        var tenThuong = ten.ToLower();
        if (await _db.NguoiDungs.AnyAsync(x => x.TenDangNhap.ToLower() == tenThuong))
            return Loi($"Tên đăng nhập '{ten}' đã tồn tại.");

        var matKhauTam = MatKhauTienIch.SinhMatKhauTam();
        var nd = new NguoiDung
        {
            TenDangNhap = ten,
            HoTen = vm.HoTen.Trim(),
            Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
            VaiTroId = vaiTro.Id,
            ToChucId = toChucId,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        nd.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(nd, matKhauTam);
        _db.NguoiDungs.Add(nd);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return Loi("Không lưu được (có thể trùng tên đăng nhập). Vui lòng kiểm tra lại.");
        }
        return new KetQuaTaoNguoiDung(true, null, matKhauTam, nd.TenDangNhap, nd.HoTen);
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(NguoiDungSuaViewModel vm)
    {
        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (u is null) return KetQuaThaoTac.Loi_("Không tìm thấy người dùng.");

        var maVaiTro = await _db.VaiTros.Where(v => v.Id == u.VaiTroId).Select(v => v.MaVaiTro).FirstOrDefaultAsync();
        if (maVaiTro == VaiTroMa.ToChucCap)
        {
            if (vm.ToChucId is null || !await _db.ToChucs.AnyAsync(t => t.Id == vm.ToChucId))
                return KetQuaThaoTac.Loi_("Nhân viên tổ chức cấp phải thuộc một tổ chức hợp lệ.");
            u.ToChucId = vm.ToChucId;
        }

        u.HoTen = vm.HoTen.Trim();
        u.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();   // vai trò không đổi được
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return KetQuaThaoTac.Loi_("Không lưu được. Vui lòng kiểm tra lại.");
        }
        TaiKhoanBoNho.Xoa(_cache, u.Id);
        return KetQuaThaoTac.Ok();
    }

    public async Task<KetQuaThaoTac> DoiTrangThaiAsync(int id, bool hoatDong, int nguoiThucHienId)
    {
        if (id == nguoiThucHienId && !hoatDong)
            return KetQuaThaoTac.Loi_("Bạn không thể tự khóa tài khoản của chính mình.");

        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return KetQuaThaoTac.Loi_("Không tìm thấy người dùng.");

        u.TrangThai = hoatDong;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return KetQuaThaoTac.Loi_("Không lưu được. Vui lòng thử lại.");
        }
        TaiKhoanBoNho.Xoa(_cache, id);   // khóa có hiệu lực ngay, kể cả phiên đang đăng nhập
        return KetQuaThaoTac.Ok();
    }

    public async Task<KetQuaTaoNguoiDung> DatLaiMatKhauAsync(int id, int nguoiThucHienId)
    {
        if (id == nguoiThucHienId)
            return Loi("Hãy dùng mục \"Đổi mật khẩu\" để đổi mật khẩu của chính bạn.");

        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return Loi("Không tìm thấy người dùng.");

        var matKhauTam = MatKhauTienIch.SinhMatKhauTam();
        u.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(u, matKhauTam);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return Loi("Không lưu được. Vui lòng thử lại.");
        }
        return new KetQuaTaoNguoiDung(true, null, matKhauTam, u.TenDangNhap, u.HoTen);
    }

    private static KetQuaTaoNguoiDung Loi(string loi) => new(false, loi, null, null, null);
}
