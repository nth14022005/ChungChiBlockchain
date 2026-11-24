using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Models.ViewModels;

// ------------------------------------------------------------------ Tổ chức
public class ToChucFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã tổ chức")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Mã tổ chức từ 2 đến 10 ký tự")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu và số")]
    [Display(Name = "Mã tổ chức")]
    public string MaToChuc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên tổ chức")]
    [StringLength(200, ErrorMessage = "Tên tối đa 200 ký tự")]
    [Display(Name = "Tên tổ chức")]
    public string TenToChuc { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự")]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    [Display(Name = "Email liên hệ")]
    public string? EmailLienHe { get; set; }

    [Url(ErrorMessage = "Website không hợp lệ (ví dụ https://example.edu.vn)")]
    [StringLength(200)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = TrangThaiToChuc.HoatDong;

    public IEnumerable<SelectListItem> DanhSachTrangThai =>
        TrangThaiToChuc.TatCa.Select(t => new SelectListItem(TrangThaiToChuc.Nhan(t), t, t == TrangThai));
}

// ------------------------------------------------------------------ Ví cấp chứng chỉ
public class ViCapFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tổ chức")]
    [Display(Name = "Tổ chức")]
    public int? ToChucId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ ví")]
    [RegularExpression("^0x[0-9a-fA-F]{40}$", ErrorMessage = "Địa chỉ ví phải có dạng 0x + 40 ký tự hex")]
    [Display(Name = "Địa chỉ ví")]
    public string DiaChiVi { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Nhãn tối đa 100 ký tự")]
    [Display(Name = "Nhãn (tên gợi nhớ)")]
    public string? Nhan { get; set; }

    public IEnumerable<SelectListItem> DanhSachToChuc { get; set; } = Enumerable.Empty<SelectListItem>();
}

// ------------------------------------------------------------------ Người dùng
public class NguoiDungTaoViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.\-]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số và các ký tự _ . -")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = VaiTroMa.ToChucCap;

    [Display(Name = "Tổ chức")]
    public int? ToChucId { get; set; }

    public IEnumerable<SelectListItem> DanhSachToChuc { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> DanhSachVaiTro => new[]
    {
        new SelectListItem("Nhân viên tổ chức cấp", VaiTroMa.ToChucCap, VaiTro == VaiTroMa.ToChucCap),
        new SelectListItem("Quản trị hệ thống", VaiTroMa.QuanTri, VaiTro == VaiTroMa.QuanTri)
    };
}

public class NguoiDungSuaViewModel
{
    public int Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Tổ chức")]
    public int? ToChucId { get; set; }

    public IEnumerable<SelectListItem> DanhSachToChuc { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class MatKhauTamThoiViewModel
{
    public string TieuDe { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class DoiMatKhauViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string MatKhauHienTai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới từ 8 ký tự trở lên")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [DataType(DataType.Password)]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string XacNhanMatKhauMoi { get; set; } = string.Empty;
}
