using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public class NguoiHocItem
{
    public int Id { get; set; }
    public string MaNguoiHoc { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly? NgaySinh { get; set; }
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
}

public record LoiNhapCsv(int Dong, string ThongBao);

public class KetQuaNhapCsv
{
    public bool ThanhCong { get; set; }
    public int SoDongNhap { get; set; }
    public int TongSoLoi { get; set; }
    public string? ThongBaoChung { get; set; }
    public List<LoiNhapCsv> Loi { get; set; } = new();
}

/// <summary>Người học: dữ liệu cá nhân, CHỈ lưu trong CSDL (không đưa lên blockchain). Mọi hàm lọc theo tổ chức của người đăng nhập.</summary>
public interface INguoiHocService
{
    Task<(List<NguoiHocItem> Items, int TongSo)> LietKeAsync(int toChucId, string? tuKhoa, int trang, int kichThuoc);
    Task<NguoiHocFormViewModel?> LayFormAsync(int toChucId, int id);
    Task<KetQuaThaoTac> TaoAsync(int toChucId, NguoiHocFormViewModel vm);
    Task<KetQuaThaoTac> CapNhatAsync(int toChucId, NguoiHocFormViewModel vm);
    Task<KetQuaNhapCsv> NhapCsvAsync(int toChucId, Stream noiDung);
}

public class NguoiHocService : INguoiHocService
{
    public const int SoDongToiDa = 2000;
    private const int SoLoiHienThi = 50;

    private static readonly string[] DinhDangNgay = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
    private static readonly Regex MauMa = new(@"^[A-Za-z0-9_.\-]{1,30}$", RegexOptions.Compiled);
    private static readonly Regex MauEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex MauDienThoai = new(@"^[0-9+\-\s.()]{6,20}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;

    public NguoiHocService(AppDbContext db) => _db = db;

    // ------------------------------------------------------------------ danh sách
    public async Task<(List<NguoiHocItem> Items, int TongSo)> LietKeAsync(
        int toChucId, string? tuKhoa, int trang, int kichThuoc)
    {
        trang = Math.Max(1, trang);
        var q = _db.NguoiHocs.AsNoTracking().Where(x => x.ToChucId == toChucId);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = tuKhoa.Trim();
            q = q.Where(x => x.HoTen.Contains(k) || x.MaNguoiHoc.Contains(k));
        }

        var tong = await q.CountAsync();
        var items = await q.OrderBy(x => x.MaNguoiHoc)
            .Skip((trang - 1) * kichThuoc).Take(kichThuoc)
            .Select(x => new NguoiHocItem
            {
                Id = x.Id, MaNguoiHoc = x.MaNguoiHoc, HoTen = x.HoTen, NgaySinh = x.NgaySinh,
                Email = x.Email, SoDienThoai = x.SoDienThoai
            }).ToListAsync();
        return (items, tong);
    }

    public async Task<NguoiHocFormViewModel?> LayFormAsync(int toChucId, int id)
    {
        var x = await _db.NguoiHocs.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id && n.ToChucId == toChucId);
        return x is null ? null : new NguoiHocFormViewModel
        {
            Id = x.Id, MaNguoiHoc = x.MaNguoiHoc, HoTen = x.HoTen,
            NgaySinh = x.NgaySinh?.ToDateTime(TimeOnly.MinValue), Email = x.Email, SoDienThoai = x.SoDienThoai
        };
    }

    // ------------------------------------------------------------------ thêm / sửa
    public async Task<KetQuaThaoTac> TaoAsync(int toChucId, NguoiHocFormViewModel vm)
    {
        var loiNgay = KiemTraNgaySinh(vm.NgaySinh);
        if (loiNgay is not null) return KetQuaThaoTac.Loi_(loiNgay);

        var ma = vm.MaNguoiHoc.Trim().ToUpperInvariant();
        if (await _db.NguoiHocs.AnyAsync(x => x.ToChucId == toChucId && x.MaNguoiHoc == ma))
            return KetQuaThaoTac.Loi_($"Mã người học '{ma}' đã tồn tại trong tổ chức của bạn.");

        _db.NguoiHocs.Add(new NguoiHoc
        {
            ToChucId = toChucId,
            MaNguoiHoc = ma,
            HoTen = vm.HoTen.Trim(),
            NgaySinh = vm.NgaySinh is null ? null : DateOnly.FromDateTime(vm.NgaySinh.Value),
            Email = Rong(vm.Email),
            SoDienThoai = Rong(vm.SoDienThoai),
            NgayTao = DateTime.Now
        });
        return await LuuAsync();
    }

    public async Task<KetQuaThaoTac> CapNhatAsync(int toChucId, NguoiHocFormViewModel vm)
    {
        var loiNgay = KiemTraNgaySinh(vm.NgaySinh);
        if (loiNgay is not null) return KetQuaThaoTac.Loi_(loiNgay);

        var n = await _db.NguoiHocs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.ToChucId == toChucId);
        if (n is null) return KetQuaThaoTac.Loi_("Không tìm thấy người học.");

        // Mã người học không đổi (đã gắn với chứng chỉ)
        n.HoTen = vm.HoTen.Trim();
        n.NgaySinh = vm.NgaySinh is null ? null : DateOnly.FromDateTime(vm.NgaySinh.Value);
        n.Email = Rong(vm.Email);
        n.SoDienThoai = Rong(vm.SoDienThoai);
        return await LuuAsync();
    }

    // ------------------------------------------------------------------ nhập CSV (tất cả hoặc không gì cả)
    public async Task<KetQuaNhapCsv> NhapCsvAsync(int toChucId, Stream noiDung)
    {
        var kq = new KetQuaNhapCsv();

        string text;
        using (var sr = new StreamReader(noiDung, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true))
            text = await sr.ReadToEndAsync();

        List<(int Dong, string[] Cot)> bg;
        try
        {
            bg = CsvTienIch.PhanTich(text, CsvTienIch.PhatHienPhanCach(text));
        }
        catch (FormatException ex)
        {
            kq.ThongBaoChung = ex.Message;
            return kq;
        }

        if (bg.Count == 0) { kq.ThongBaoChung = "File rỗng."; return kq; }

        var tieuDe = bg[0].Cot.Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var viTri = new Dictionary<string, int>();
        for (var i = 0; i < tieuDe.Length; i++) viTri.TryAdd(tieuDe[i], i);

        var thieu = new[] { "ma_nguoi_hoc", "ho_ten" }.Where(c => !viTri.ContainsKey(c)).ToList();
        if (thieu.Count > 0)
        {
            kq.ThongBaoChung = "Thiếu cột bắt buộc: " + string.Join(", ", thieu) +
                ". Các cột hợp lệ: ma_nguoi_hoc, ho_ten, ngay_sinh, email, so_dien_thoai. Hãy tải file mẫu.";
            return kq;
        }

        var dong = bg.Skip(1).ToList();
        if (dong.Count == 0) { kq.ThongBaoChung = "File chỉ có dòng tiêu đề, không có dữ liệu."; return kq; }
        if (dong.Count > SoDongToiDa)
        {
            kq.ThongBaoChung = $"File có {dong.Count} dòng dữ liệu, vượt giới hạn {SoDongToiDa} dòng mỗi lần. Hãy chia nhỏ file.";
            return kq;
        }

        string Lay(string[] cot, string ten) =>
            viTri.TryGetValue(ten, out var i) && i < cot.Length ? cot[i].Trim() : string.Empty;

        var loi = new List<LoiNhapCsv>();
        var hopLe = new List<NguoiHoc>();
        var maDaThay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        foreach (var (soDong, cot) in dong)
        {
            var loiDong = new List<string>();
            var ma = Lay(cot, "ma_nguoi_hoc").ToUpperInvariant();
            var hoTen = Lay(cot, "ho_ten");
            var ngay = Lay(cot, "ngay_sinh");
            var email = Lay(cot, "email");
            var sdt = Lay(cot, "so_dien_thoai");

            if (!MauMa.IsMatch(ma)) loiDong.Add("mã người học không hợp lệ (chữ không dấu, số, _ . -, tối đa 30 ký tự)");
            else if (maDaThay.TryGetValue(ma, out var dongTruoc)) loiDong.Add($"mã '{ma}' bị trùng với dòng {dongTruoc} trong file");
            else maDaThay[ma] = soDong;

            if (hoTen.Length == 0) loiDong.Add("thiếu họ tên");
            else if (hoTen.Length > 100) loiDong.Add("họ tên quá dài (tối đa 100 ký tự)");

            DateOnly? ngaySinh = null;
            if (ngay.Length > 0)
            {
                if (!DateOnly.TryParseExact(ngay, DinhDangNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    loiDong.Add("ngày sinh không đúng định dạng (dd/MM/yyyy hoặc yyyy-MM-dd)");
                else if (d.Year < 1900 || d > homNay)
                    loiDong.Add("ngày sinh không hợp lệ (phải từ năm 1900 đến hôm nay)");
                else ngaySinh = d;
            }

            if (email.Length > 0 && (email.Length > 100 || !MauEmail.IsMatch(email))) loiDong.Add("email không hợp lệ");
            if (sdt.Length > 0 && !MauDienThoai.IsMatch(sdt)) loiDong.Add("số điện thoại không hợp lệ");

            if (loiDong.Count > 0)
            {
                loi.Add(new LoiNhapCsv(soDong, string.Join("; ", loiDong)));
                continue;
            }

            hopLe.Add(new NguoiHoc
            {
                ToChucId = toChucId, MaNguoiHoc = ma, HoTen = hoTen, NgaySinh = ngaySinh,
                Email = email.Length > 0 ? email : null, SoDienThoai = sdt.Length > 0 ? sdt : null,
                NgayTao = DateTime.Now
            });
        }

        // Mã đã có trong CSDL của tổ chức này
        var maKiemTra = hopLe.Select(x => x.MaNguoiHoc).ToList();
        if (maKiemTra.Count > 0)
        {
            var daCo = (await _db.NguoiHocs
                    .Where(x => x.ToChucId == toChucId && maKiemTra.Contains(x.MaNguoiHoc))
                    .Select(x => x.MaNguoiHoc).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var m in hopLe.Where(x => daCo.Contains(x.MaNguoiHoc)))
                loi.Add(new LoiNhapCsv(maDaThay[m.MaNguoiHoc], $"mã '{m.MaNguoiHoc}' đã tồn tại trong hệ thống"));
        }

        if (loi.Count > 0)
        {
            loi = loi.OrderBy(l => l.Dong).ToList();
            kq.TongSoLoi = loi.Count;
            kq.Loi = loi.Take(SoLoiHienThi).ToList();
            kq.ThongBaoChung = "Không có người học nào được nhập vì file còn lỗi. Hãy sửa file rồi tải lên lại.";
            return kq;
        }

        foreach (var n in hopLe) _db.NguoiHocs.Add(n);
        var luu = await LuuAsync();
        if (!luu.ThanhCong)
        {
            kq.ThongBaoChung = luu.Loi;
            return kq;
        }

        kq.ThanhCong = true;
        kq.SoDongNhap = hopLe.Count;
        return kq;
    }

    // ------------------------------------------------------------------ nội bộ
    private static string? KiemTraNgaySinh(DateTime? ngay)
    {
        if (ngay is null) return null;
        var d = DateOnly.FromDateTime(ngay.Value);
        return d.Year < 1900 || d > DateOnly.FromDateTime(DateTime.Today)
            ? "Ngày sinh không hợp lệ (phải từ năm 1900 đến hôm nay)."
            : null;
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
            return KetQuaThaoTac.Loi_("Không lưu được (có thể bị trùng mã hoặc dữ liệu quá dài).");
        }
    }

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
