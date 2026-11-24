using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class NhatKyHoatDong
{
    public int Id { get; set; }

    public int? NguoiDungId { get; set; }

    public string HanhDong { get; set; } = null!;

    public string? DoiTuong { get; set; }

    public string? ChiTiet { get; set; }

    public string? DiaChiIp { get; set; }

    public DateTime ThoiGian { get; set; }

    public virtual NguoiDung? NguoiDung { get; set; }
}
