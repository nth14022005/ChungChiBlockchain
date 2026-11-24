using System.Text.RegularExpressions;

namespace ChungChiBlockchain.Web.Services.Blockchain;

/// <summary>Trạng thái chứng chỉ trên chuỗi. Giá trị số khớp enum Status của smart contract.</summary>
public enum TrangThaiTrenChuoi
{
    KhongTimThay = 0,
    HopLe = 1,
    DaThuHoi = 2
}

public enum TrangThaiGiaoDich
{
    KhongTimThay = 0,
    DangCho = 1,
    ThanhCong = 2,
    ThatBai = 3
}

public record KetQuaTraCuuChuoi(
    TrangThaiTrenChuoi TrangThai,
    string? ViCap,
    DateTimeOffset? ThoiGianCap,
    DateTimeOffset? ThoiGianThuHoi);

public record KetQuaGiaoDich(TrangThaiGiaoDich TrangThai, long? SoKhoi, string? ThongBaoLoi);

/// <summary>
/// Khớp nối với blockchain (xem QUYET_DINH_THIET_KE.md, mục 4).
/// Server CHỈ ĐỌC chuỗi. Việc ký giao dịch cấp/thu hồi diễn ra trong MetaMask ở trình duyệt,
/// nên server không bao giờ giữ khóa riêng của ví.
/// </summary>
public interface IBlockchainService
{
    bool LaGiaLap { get; }
    string TenMang { get; }

    Task<bool> SanSangAsync(CancellationToken ct = default);

    /// <summary>Tra cứu một hash (0x + 64 ký tự hex) trên chuỗi.</summary>
    Task<KetQuaTraCuuChuoi> TraCuuAsync(string certHash, CancellationToken ct = default);

    /// <summary>Kiểm tra giao dịch đã được xác nhận chưa (dùng cho luồng Pending → Success).</summary>
    Task<KetQuaGiaoDich> LayTrangThaiGiaoDichAsync(string txHash, CancellationToken ct = default);
}

public class BlockchainOptions
{
    public string Mode { get; set; } = "GiaLap";
    public string TenMang { get; set; } = "Giả lập";
    public string DuongDanDuLieuGiaLap { get; set; } = "../../storage/blockchain-gia-lap.json";
    public int DoTreXacNhanGiay { get; set; } = 0;
}

/// <summary>Quy tắc định dạng hash: 0x + 64 ký tự hex, chuẩn hóa về chữ thường.</summary>
public static class DinhDangHash
{
    private static readonly Regex Mau = new("^0x[0-9a-fA-F]{64}$", RegexOptions.Compiled);

    public static bool HopLe(string? hash) => hash is not null && Mau.IsMatch(hash);

    public static string? ChuanHoa(string? hash) => HopLe(hash) ? hash!.ToLowerInvariant() : null;

    public static string ChuanHoaHoacLoi(string? hash) =>
        ChuanHoa(hash) ?? throw new ArgumentException("Hash không hợp lệ (cần 0x + 64 ký tự hex).", nameof(hash));
}
