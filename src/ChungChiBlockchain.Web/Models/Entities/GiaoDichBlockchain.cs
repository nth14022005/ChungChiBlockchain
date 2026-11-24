using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class GiaoDichBlockchain
{
    public int Id { get; set; }

    public int ChungChiId { get; set; }

    public int HopDongId { get; set; }

    public int? ViId { get; set; }

    public string Loai { get; set; } = null!;

    public string? TxHash { get; set; }

    public long? SoKhoi { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? ThongBaoLoi { get; set; }

    public DateTime NgayGui { get; set; }

    public DateTime? NgayXacNhan { get; set; }

    public virtual ChungChi ChungChi { get; set; } = null!;

    public virtual PhienBanHopDong HopDong { get; set; } = null!;

    public virtual ICollection<ThuHoiChungChi> ThuHoiChungChis { get; set; } = new List<ThuHoiChungChi>();

    public virtual ViBlockchain? Vi { get; set; }
}
