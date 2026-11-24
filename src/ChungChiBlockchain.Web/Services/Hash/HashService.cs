using System.Security.Cryptography;
using ChungChiBlockchain.Web.Services.Blockchain;

namespace ChungChiBlockchain.Web.Services.Hash;

/// <summary>
/// Khớp nối băm (xem QUYET_DINH_THIET_KE.md, mục 3.1 và 4).
/// Quy tắc phiên bản 1: certHash = "0x" + SHA-256(toàn bộ byte của file PDF cuối cùng), chữ thường, 66 ký tự.
/// Đổi quy tắc băm sau này phải tăng PhienBanBam, không sửa hàm này để chứng chỉ cũ vẫn xác minh được.
/// </summary>
public interface IHashService
{
    /// <summary>Phiên bản quy tắc băm đang dùng, ghi vào cột phien_ban_bam.</summary>
    byte PhienBanHienTai { get; }

    /// <summary>Băm toàn bộ nội dung file. Trả về dạng 0x + 64 ký tự hex chữ thường.</summary>
    string BamNoiDung(ReadOnlySpan<byte> noiDung);
}

public class HashServiceV1 : IHashService
{
    public byte PhienBanHienTai => 1;

    public string BamNoiDung(ReadOnlySpan<byte> noiDung)
    {
        var digest = SHA256.HashData(noiDung);
        return "0x" + Convert.ToHexStringLower(digest);
    }
}

/// <summary>Kiểm tra định dạng hash để dùng ở mọi nơi nhận hash từ người dùng hoặc từ blockchain.</summary>
public static class DinhDangHashTienIch
{
    public static bool HopLe(string? hash) => DinhDangHash.HopLe(hash);
    public static string? ChuanHoa(string? hash) => DinhDangHash.ChuanHoa(hash);
}
