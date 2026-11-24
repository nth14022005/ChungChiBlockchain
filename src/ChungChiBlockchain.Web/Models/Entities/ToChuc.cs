using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class ToChuc
{
    public int Id { get; set; }

    public string MaToChuc { get; set; } = null!;

    public string TenToChuc { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? EmailLienHe { get; set; }

    public string? Website { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<KhoaHoc> KhoaHocs { get; set; } = new List<KhoaHoc>();

    public virtual ICollection<LoPhatHanh> LoPhatHanhs { get; set; } = new List<LoPhatHanh>();

    public virtual ICollection<MauChungChi> MauChungChis { get; set; } = new List<MauChungChi>();

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();

    public virtual ICollection<NguoiHoc> NguoiHocs { get; set; } = new List<NguoiHoc>();

    public virtual ICollection<ViBlockchain> ViBlockchains { get; set; } = new List<ViBlockchain>();
}
