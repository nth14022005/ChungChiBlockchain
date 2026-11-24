namespace ChungChiBlockchain.Web.Models.ViewModels;

public sealed record DongDem(string Ma, int SoLuong);
public sealed record DongLichSu(DateTime ThoiGian, string? ChungChi, string PhuongThuc, string KetQua);
public sealed record DongNhatKy(DateTime ThoiGian, int? NguoiDung, string HanhDong, string? DoiTuong);
public sealed class BaoCaoHoatDong
{
    public required List<DongDem> TrangThai { get; init; }
    public required List<DongDem> XacMinh { get; init; }
    public required List<DongLichSu> LichSu { get; init; }
    public required List<DongNhatKy> NhatKy { get; init; }
    public static string Ten(string ma) => ma switch
    {
        "nhap" => "Nháp", "cho_xac_nhan" => "Chờ xác nhận", "da_ghi_chuoi" => "Đã ghi chuỗi",
        "thu_hoi" => "Đã thu hồi", "loi" => "Lỗi", "hop_le" => "Hợp lệ",
        "khong_khop" => "PDF không khớp", "da_thu_hoi" => "Đã thu hồi",
        "khong_tim_thay" => "Không tìm thấy", "ma_qr" => "Liên kết / QR",
        "tai_pdf" => "Tải PDF", "nhap_ma" => "Nhập mã", _ => ma
    };
}
