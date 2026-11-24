using System.ComponentModel.DataAnnotations;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Models.ViewModels;

public class KhoaHocFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã khóa học")]
    [StringLength(30, ErrorMessage = "Mã khóa học tối đa 30 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9_.\-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số và các ký tự _ . -")]
    [Display(Name = "Mã khóa học")]
    public string MaKhoaHoc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
    [StringLength(200, ErrorMessage = "Tên khóa học tối đa 200 ký tự")]
    [Display(Name = "Tên khóa học")]
    public string TenKhoaHoc { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Thời lượng tối đa 50 ký tự")]
    [Display(Name = "Thời lượng (ví dụ: 120 giờ)")]
    public string? ThoiLuong { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
}

public class LoaiChungChiFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại chứng chỉ")]
    [StringLength(100, ErrorMessage = "Tên loại tối đa 100 ký tự")]
    [Display(Name = "Tên loại chứng chỉ")]
    public string TenLoai { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
}

public class MauChungChiFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên mẫu")]
    [StringLength(150, ErrorMessage = "Tên mẫu tối đa 150 ký tự")]
    [Display(Name = "Tên mẫu")]
    public string TenMau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại chứng chỉ")]
    [Display(Name = "Loại chứng chỉ")]
    public int? LoaiChungChiId { get; set; }

    public IEnumerable<SelectListItem> DanhSachLoai { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class NguoiHocFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã người học")]
    [StringLength(30, ErrorMessage = "Mã người học tối đa 30 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9_.\-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số và các ký tự _ . -")]
    [Display(Name = "Mã người học")]
    public string MaNguoiHoc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [RegularExpression(@"^[0-9+\-\s.()]{6,20}$", ErrorMessage = "Số điện thoại không hợp lệ (6-20 ký tự số, + - . ( ) )")]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }
}

public class NguoiHocDanhSachViewModel
{
    public List<NguoiHocItem> Items { get; set; } = new();
    public string? TuKhoa { get; set; }
    public int Trang { get; set; } = 1;
    public int TongSo { get; set; }
    public int KichThuoc { get; set; } = 20;
    public int TongTrang => Math.Max(1, (int)Math.Ceiling(TongSo / (double)KichThuoc));
}

public class NhapCsvViewModel
{
    public bool DaChay { get; set; }
    public bool ThanhCong { get; set; }
    public int SoDongNhap { get; set; }
    public int TongSoLoi { get; set; }
    public string? ThongBaoChung { get; set; }
    public List<LoiNhapCsv> Loi { get; set; } = new();
}
