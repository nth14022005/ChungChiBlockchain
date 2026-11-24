namespace ChungChiBlockchain.Web.Models;

/// <summary>Trạng thái tổ chức, khớp ràng buộc CHECK của cột to_chuc.trang_thai.</summary>
public static class TrangThaiToChuc
{
    public const string ChoDuyet = "cho_duyet";
    public const string HoatDong = "hoat_dong";
    public const string TamKhoa = "tam_khoa";

    public static readonly string[] TatCa = { HoatDong, ChoDuyet, TamKhoa };

    public static string Nhan(string? t) => t switch
    {
        ChoDuyet => "Chờ duyệt",
        HoatDong => "Hoạt động",
        TamKhoa => "Tạm khóa",
        _ => t ?? string.Empty
    };

    public static string MauBadge(string? t) => t switch
    {
        HoatDong => "bg-success",
        ChoDuyet => "bg-warning text-dark",
        _ => "bg-danger"
    };
}

/// <summary>Trạng thái ví, khớp ràng buộc CHECK của cột vi_blockchain.trang_thai.</summary>
public static class TrangThaiVi
{
    public const string ChoDuyet = "cho_duyet";
    public const string HoatDong = "hoat_dong";
    public const string BiKhoa = "bi_khoa";

    public static readonly string[] TatCa = { ChoDuyet, HoatDong, BiKhoa };

    public static string Nhan(string? t) => t switch
    {
        ChoDuyet => "Chờ duyệt",
        HoatDong => "Hoạt động",
        BiKhoa => "Bị khóa",
        _ => t ?? string.Empty
    };

    public static string MauBadge(string? t) => t switch
    {
        HoatDong => "bg-success",
        ChoDuyet => "bg-warning text-dark",
        _ => "bg-danger"
    };
}
