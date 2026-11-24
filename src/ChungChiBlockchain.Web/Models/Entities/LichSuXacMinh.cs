using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class LichSuXacMinh
{
    public int Id { get; set; }

    public int? ChungChiId { get; set; }

    public int? DoanhNghiepId { get; set; }

    public string HashTraCuu { get; set; } = null!;

    public string KetQua { get; set; } = null!;

    public string PhuongThuc { get; set; } = null!;

    public string? DiaChiIp { get; set; }

    public DateTime ThoiGian { get; set; }

    public virtual ChungChi? ChungChi { get; set; }

    public virtual DoanhNghiepXacMinh? DoanhNghiep { get; set; }
}
