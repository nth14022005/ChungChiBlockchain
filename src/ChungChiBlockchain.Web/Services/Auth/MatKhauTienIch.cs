using System.Security.Cryptography;
using ChungChiBlockchain.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace ChungChiBlockchain.Web.Services.Auth;

public static class MatKhauTienIch
{
    /// <summary>Bộ băm mật khẩu chuẩn của ASP.NET Core (PBKDF2 có muối).</summary>
    public static readonly PasswordHasher<NguoiDung> Hasher = new();

    // Bỏ các ký tự dễ nhầm (I, O, l, o, 0, 1)
    private const string BangKyTu = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public static string SinhMatKhauTam(int doDai = 12)
    {
        var kq = new char[doDai];
        for (var i = 0; i < doDai; i++)
            kq[i] = BangKyTu[RandomNumberGenerator.GetInt32(BangKyTu.Length)];
        return new string(kq);
    }
}

/// <summary>Bộ nhớ đệm "tài khoản còn hoạt động không" để khóa tài khoản có hiệu lực ngay mà không truy vấn CSDL mỗi request.</summary>
public static class TaiKhoanBoNho
{
    public static string Key(int id) => $"taikhoan-hoatdong:{id}";
    public static void Xoa(IMemoryCache cache, int id) => cache.Remove(Key(id));
    public static void XoaNhieu(IMemoryCache cache, IEnumerable<int> ids)
    {
        foreach (var id in ids) cache.Remove(Key(id));
    }
}
