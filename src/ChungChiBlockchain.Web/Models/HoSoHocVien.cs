using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Models;

public static class VaiTroHocVien
{
    public const string Ma = "nguoi_hoc";
}

public sealed class CapTaiKhoanHocVien
{
    [Display(Name = "Hồ sơ người học"), Required(ErrorMessage = "Hãy chọn hồ sơ người học.")]
    public int? NguoiHocId { get; set; }

    [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy nhập tên đăng nhập.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$", ErrorMessage = "Dùng 3–50 chữ không dấu, số hoặc các ký tự _ . -")]
    public string TenDangNhap { get; set; } = "";
    public List<SelectListItem> HoSo { get; set; } = [];
}

public sealed record ChungChiCaNhan(int Id, string Ma, string KhoaHoc, DateOnly NgayCap, string? XepLoai, string TrangThai);
