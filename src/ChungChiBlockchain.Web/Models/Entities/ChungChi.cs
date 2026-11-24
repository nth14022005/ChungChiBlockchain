using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class ChungChi
{
    public int Id { get; set; }

    public string MaChungChi { get; set; } = null!;

    public int NguoiHocId { get; set; }

    public int KhoaHocId { get; set; }

    public int MauChungChiId { get; set; }

    public int? LoPhatHanhId { get; set; }

    public int NguoiCapId { get; set; }

    public DateOnly NgayCap { get; set; }

    public string? XepLoai { get; set; }

    public string? HashChungChi { get; set; }

    public byte PhienBanBam { get; set; }

    public string DuLieuChungChi { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<GiaoDichBlockchain> GiaoDichBlockchains { get; set; } = new List<GiaoDichBlockchain>();

    public virtual KhoaHoc KhoaHoc { get; set; } = null!;

    public virtual ICollection<LichSuXacMinh> LichSuXacMinhs { get; set; } = new List<LichSuXacMinh>();

    public virtual ICollection<LienKetChiaSe> LienKetChiaSes { get; set; } = new List<LienKetChiaSe>();

    public virtual LoPhatHanh? LoPhatHanh { get; set; }

    public virtual MauChungChi MauChungChi { get; set; } = null!;

    public virtual NguoiDung NguoiCap { get; set; } = null!;

    public virtual NguoiHoc NguoiHoc { get; set; } = null!;

    public virtual ICollection<TepChungChi> TepChungChis { get; set; } = new List<TepChungChi>();

    public virtual ThuHoiChungChi? ThuHoiChungChi { get; set; }
}
