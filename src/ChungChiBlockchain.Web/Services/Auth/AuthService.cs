using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChungChiBlockchain.Web.Services.Auth;

public record NguoiDungDangNhap(int Id, string TenDangNhap, string HoTen, string MaVaiTro, string TenVaiTro, int? ToChucId);

public record KetQuaDangNhap(bool ThanhCong, string? Loi, NguoiDungDangNhap? NguoiDung);

public record TrangThaiTaiKhoan(bool HoatDong, int? ToChucId);

public interface IAuthService
{
    Task<bool> CoNguoiDungAsync();
    Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau);
    Task<NguoiDungDangNhap> TaoQuanTriDauTienAsync(string tenDangNhap, string matKhau, string hoTen, string? email);

    /// <summary>Tình trạng hiện tại của tài khoản trong CSDL (có bộ nhớ đệm ngắn): còn hoạt động không, thuộc tổ chức nào.</summary>
    Task<TrangThaiTaiKhoan> LayTrangThaiAsync(int id);

    Task<KetQuaThaoTac> DoiMatKhauAsync(int id, string matKhauCu, string matKhauMoi);
}

public class AuthService : IAuthService
{
    private const string LoiChung = "Sai tên đăng nhập hoặc mật khẩu.";
    private static readonly TimeSpan ThoiGianNho = TimeSpan.FromSeconds(30);

    // Băm giả để thời gian xử lý như nhau dù tên đăng nhập có tồn tại hay không
    private static readonly string DummyHash =
        MatKhauTienIch.Hasher.HashPassword(new NguoiDung(), "mat-khau-gia-de-can-bang-thoi-gian");

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public AuthService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<bool> CoNguoiDungAsync() => _db.NguoiDungs.AnyAsync();

    public async Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var ten = (tenDangNhap ?? string.Empty).Trim();

        var u = await _db.NguoiDungs
            .Where(x => x.TenDangNhap == ten)
            .Select(x => new
            {
                x.Id, x.TenDangNhap, x.MatKhauHash, x.HoTen, x.TrangThai, x.ToChucId,
                x.VaiTro.MaVaiTro, x.VaiTro.TenVaiTro,
                ToChucTrangThai = x.ToChuc != null ? x.ToChuc.TrangThai : null
            })
            .FirstOrDefaultAsync();

        var ketQua = MatKhauTienIch.Hasher.VerifyHashedPassword(
            new NguoiDung(), u?.MatKhauHash ?? DummyHash, matKhau ?? string.Empty);

        if (u is null || ketQua == PasswordVerificationResult.Failed)
            return new KetQuaDangNhap(false, LoiChung, null);

        // Chỉ báo lý do cụ thể khi đã đúng mật khẩu, để không lộ tài khoản nào tồn tại
        if (!u.TrangThai)
            return new KetQuaDangNhap(false, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.", null);
        if (u.ToChucId is not null && u.ToChucTrangThai != TrangThaiToChuc.HoatDong)
            return new KetQuaDangNhap(false,
                "Tổ chức của bạn đang tạm khóa hoặc chưa được duyệt. Vui lòng liên hệ quản trị viên.", null);

        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var entity = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Id == u.Id);
            if (entity is not null)
            {
                entity.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(entity, matKhau!);
                await _db.SaveChangesAsync();
            }
        }

        TaiKhoanBoNho.Xoa(_cache, u.Id);
        return new KetQuaDangNhap(true, null,
            new NguoiDungDangNhap(u.Id, u.TenDangNhap, u.HoTen, u.MaVaiTro, u.TenVaiTro, u.ToChucId));
    }

    public async Task<NguoiDungDangNhap> TaoQuanTriDauTienAsync(
        string tenDangNhap, string matKhau, string hoTen, string? email)
    {
        if (await _db.NguoiDungs.AnyAsync())
            throw new InvalidOperationException("Hệ thống đã có người dùng, không thể dùng chức năng khởi tạo.");

        var vaiTro = await _db.VaiTros.FirstOrDefaultAsync(v => v.MaVaiTro == VaiTroMa.QuanTri)
            ?? throw new InvalidOperationException(
                "Chưa có vai trò 'quan_tri' trong CSDL. Hãy chạy lại script SQL tạo CSDL.");

        var nd = new NguoiDung
        {
            TenDangNhap = tenDangNhap.Trim(),
            HoTen = hoTen.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            VaiTroId = vaiTro.Id,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        nd.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(nd, matKhau);

        _db.NguoiDungs.Add(nd);
        await _db.SaveChangesAsync();

        return new NguoiDungDangNhap(nd.Id, nd.TenDangNhap, nd.HoTen, vaiTro.MaVaiTro, vaiTro.TenVaiTro, null);
    }

    public async Task<TrangThaiTaiKhoan> LayTrangThaiAsync(int id)
    {
        if (_cache.TryGetValue(TaiKhoanBoNho.Key(id), out TrangThaiTaiKhoan? daBiet) && daBiet is not null)
            return daBiet;

        var t = await _db.NguoiDungs
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.TrangThai, x.ToChucId,
                ToChucTrangThai = x.ToChuc != null ? x.ToChuc.TrangThai : null
            })
            .FirstOrDefaultAsync();

        var hoatDong = t is not null && t.TrangThai &&
                       (t.ToChucId is null || t.ToChucTrangThai == TrangThaiToChuc.HoatDong);
        var kq = new TrangThaiTaiKhoan(hoatDong, t?.ToChucId);

        _cache.Set(TaiKhoanBoNho.Key(id), kq, ThoiGianNho);
        return kq;
    }

    public async Task<KetQuaThaoTac> DoiMatKhauAsync(int id, string matKhauCu, string matKhauMoi)
    {
        if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 8)
            return KetQuaThaoTac.Loi_("Mật khẩu mới phải từ 8 ký tự trở lên.");

        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return KetQuaThaoTac.Loi_("Không tìm thấy tài khoản.");

        if (MatKhauTienIch.Hasher.VerifyHashedPassword(u, u.MatKhauHash, matKhauCu ?? string.Empty)
            == PasswordVerificationResult.Failed)
            return KetQuaThaoTac.Loi_("Mật khẩu hiện tại không đúng.");

        if (matKhauMoi == matKhauCu)
            return KetQuaThaoTac.Loi_("Mật khẩu mới phải khác mật khẩu hiện tại.");

        u.MatKhauHash = MatKhauTienIch.Hasher.HashPassword(u, matKhauMoi);
        await _db.SaveChangesAsync();
        return KetQuaThaoTac.Ok();
    }
}
