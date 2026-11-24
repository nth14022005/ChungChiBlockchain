using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class DoanhNghiepXacMinh
{
    public int Id { get; set; }

    public int NguoiDungId { get; set; }

    public string TenDoanhNghiep { get; set; } = null!;

    public string? MaSoThue { get; set; }

    public string? NguoiLienHe { get; set; }

    public DateTime NgayDangKy { get; set; }

    public virtual ICollection<LichSuXacMinh> LichSuXacMinhs { get; set; } = new List<LichSuXacMinh>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
