using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChungChiBlockchain.Web.Services.Blockchain;

/// <summary>
/// Chuỗi khối GIẢ LẬP để phát triển và demo dự phòng khi mạng thật lỗi.
/// Mô phỏng hành vi smart contract: cấp trùng hash bị từ chối, thu hồi chỉ khi đã cấp,
/// giao dịch có thể "đang chờ" một khoảng thời gian (cấu hình DoTreXacNhanGiay).
/// Dữ liệu lưu ở file JSON trong thư mục storage/ (không đưa lên Git).
/// </summary>
public class GiaLapBlockchainService : IBlockchainService
{
    public const string ViGiaLap = "0x0000000000000000000000000000000000000001";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly object _khoa = new();
    private readonly string _duongDan;
    private readonly int _doTreGiay;
    private readonly ILogger<GiaLapBlockchainService> _logger;
    private DuLieuGiaLap _du = new();

    public GiaLapBlockchainService(
        IOptions<BlockchainOptions> options,
        IWebHostEnvironment env,
        ILogger<GiaLapBlockchainService> logger)
    {
        var opt = options.Value;
        _logger = logger;
        _doTreGiay = Math.Max(0, opt.DoTreXacNhanGiay);
        TenMang = opt.TenMang;
        _duongDan = Path.GetFullPath(Path.Combine(env.ContentRootPath, opt.DuongDanDuLieuGiaLap));
        Tai();
    }

    public bool LaGiaLap => true;
    public string TenMang { get; }

    public Task<bool> SanSangAsync(CancellationToken ct = default) => Task.FromResult(true);

    // ------------------------------------------------------------------ đọc
    public Task<KetQuaTraCuuChuoi> TraCuuAsync(string certHash, CancellationToken ct = default)
    {
        var h = DinhDangHash.ChuanHoa(certHash);
        var khongThay = new KetQuaTraCuuChuoi(TrangThaiTrenChuoi.KhongTimThay, null, null, null);
        if (h is null) return Task.FromResult(khongThay);

        var now = DateTimeOffset.UtcNow;
        lock (_khoa)
        {
            if (!_du.BanGhi.TryGetValue(h, out var b) || b.ThoiGianCap > now)
                return Task.FromResult(khongThay);

            var daThuHoi = b.ThoiGianThuHoi is not null && b.ThoiGianThuHoi <= now;
            return Task.FromResult(new KetQuaTraCuuChuoi(
                daThuHoi ? TrangThaiTrenChuoi.DaThuHoi : TrangThaiTrenChuoi.HopLe,
                b.ViCap, b.ThoiGianCap, daThuHoi ? b.ThoiGianThuHoi : null));
        }
    }

    public Task<KetQuaGiaoDich> LayTrangThaiGiaoDichAsync(string txHash, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (string.IsNullOrWhiteSpace(txHash) || !_du.GiaoDich.TryGetValue(txHash.ToLowerInvariant(), out var g))
                return Task.FromResult(new KetQuaGiaoDich(TrangThaiGiaoDich.KhongTimThay, null, null));

            return Task.FromResult(g.XacNhanLuc > DateTimeOffset.UtcNow
                ? new KetQuaGiaoDich(TrangThaiGiaoDich.DangCho, null, null)
                : new KetQuaGiaoDich(TrangThaiGiaoDich.ThanhCong, g.SoKhoi, null));
        }
    }

    // ------------------------------------------------- ghi (chỉ có ở bản giả lập)
    /// <summary>Giả lập hàm issue() của contract. Trả về mã giao dịch giả.</summary>
    public Task<string> GhiCapAsync(string certHash, string? viCap = null, string? requestTx = null)
    {
        var h = DinhDangHash.ChuanHoaHoacLoi(certHash);
        lock (_khoa)
        {
            if (requestTx is not null && _du.GiaoDich.TryGetValue(requestTx, out var existing)) { if(existing.CertHash != h || existing.Loai != "cap") throw new InvalidOperationException("Mã yêu cầu không khớp."); return Task.FromResult(requestTx); }
            if (_du.BanGhi.ContainsKey(h))
                throw new InvalidOperationException("Chứng chỉ đã tồn tại trên chuỗi (giả lập revert).");

            var xacNhan = DateTimeOffset.UtcNow.AddSeconds(_doTreGiay);
            _du.SoKhoi++;
            var tx = requestTx ?? TaoTxHash("cap", h);
            _du.BanGhi[h] = new BanGhi { ViCap = viCap ?? ViGiaLap, ThoiGianCap = xacNhan };
            _du.GiaoDich[tx] = new GiaoDichGiaLap { Loai = "cap", CertHash = h, SoKhoi = _du.SoKhoi, XacNhanLuc = xacNhan };
            try { Luu(); } catch { Tai(); throw; }
            return Task.FromResult(tx);
        }
    }

    /// <summary>Giả lập hàm revoke() của contract.</summary>
    public Task<string> GhiThuHoiAsync(string certHash, string? viCap = null, string? requestTx = null)
    {
        var h = DinhDangHash.ChuanHoaHoacLoi(certHash);
        var now = DateTimeOffset.UtcNow;
        lock (_khoa)
        {
            if (requestTx is not null && _du.GiaoDich.TryGetValue(requestTx, out var existing)) { if(existing.CertHash != h || existing.Loai != "thu_hoi") throw new InvalidOperationException("Mã yêu cầu không khớp."); return Task.FromResult(requestTx); }
            if (!_du.BanGhi.TryGetValue(h, out var b) || b.ThoiGianCap > now)
                throw new InvalidOperationException("Chứng chỉ không tồn tại (giả lập revert).");
            if (b.ThoiGianThuHoi is not null)
                throw new InvalidOperationException("Chứng chỉ đã bị thu hồi trước đó (giả lập revert).");
            if (viCap is not null && !string.Equals(viCap, b.ViCap, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ tổ chức đã cấp mới được thu hồi (giả lập revert).");

            var xacNhan = now.AddSeconds(_doTreGiay);
            _du.SoKhoi++;
            var tx = requestTx ?? TaoTxHash("thu_hoi", h);
            b.ThoiGianThuHoi = xacNhan;
            _du.GiaoDich[tx] = new GiaoDichGiaLap { Loai = "thu_hoi", CertHash = h, SoKhoi = _du.SoKhoi, XacNhanLuc = xacNhan };
            try { Luu(); } catch { Tai(); throw; }
            return Task.FromResult(tx);
        }
    }

    // ------------------------------------------------------------- nội bộ
    private string TaoTxHash(string loai, string certHash)
    {
        var raw = Encoding.UTF8.GetBytes($"{loai}|{certHash}|{_du.SoKhoi}|{DateTime.UtcNow.Ticks}");
        return "0x" + Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
    }

    private void Tai()
    {
        try
        {
            if (!File.Exists(_duongDan)) { _du = new(); return; }
            if (File.Exists(_duongDan))
                _du = JsonSerializer.Deserialize<DuLieuGiaLap>(File.ReadAllText(_duongDan)) ?? new DuLieuGiaLap();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            throw new InvalidOperationException("Không đọc được dữ liệu giả lập. Khôi phục bản sao lưu; không tự xóa dữ liệu chuỗi.", ex);
        }
    }

    private void Luu()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_duongDan)!);
        var tam = _duongDan + ".tmp";
        File.WriteAllText(tam, JsonSerializer.Serialize(_du, JsonOpts));
        File.Move(tam, _duongDan, overwrite: true);
    }

    private class DuLieuGiaLap
    {
        public long SoKhoi { get; set; }
        public Dictionary<string, BanGhi> BanGhi { get; set; } = new();
        public Dictionary<string, GiaoDichGiaLap> GiaoDich { get; set; } = new();
    }

    private class BanGhi
    {
        public string ViCap { get; set; } = string.Empty;
        public DateTimeOffset ThoiGianCap { get; set; }
        public DateTimeOffset? ThoiGianThuHoi { get; set; }
    }

    private class GiaoDichGiaLap
    {
        public string Loai { get; set; } = string.Empty;
        public string CertHash { get; set; } = string.Empty;
        public long SoKhoi { get; set; }
        public DateTimeOffset XacNhanLuc { get; set; }
    }
}
