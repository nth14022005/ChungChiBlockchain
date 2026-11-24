using System.Security.Claims;

namespace ChungChiBlockchain.Web.Services;

public record KetQuaThaoTac(bool ThanhCong, string? Loi = null)
{
    public static KetQuaThaoTac Ok() => new(true);
    public static KetQuaThaoTac Loi_(string loi) => new(false, loi);
}

public record KetQuaTaoNguoiDung(bool ThanhCong, string? Loi, string? MatKhauTam, string? TenDangNhap, string? HoTen);

public static class NguoiDungHienTai
{
    public static int? LayId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Tổ chức của người đang đăng nhập (từ claim). Dữ liệu danh mục luôn được lọc theo giá trị này, không bao giờ theo dữ liệu form.</summary>
    public static int? LayToChucId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("to_chuc_id"), out var id) ? id : null;
}
