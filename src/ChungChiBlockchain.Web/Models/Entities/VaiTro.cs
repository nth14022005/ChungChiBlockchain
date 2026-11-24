using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class VaiTro
{
    public int Id { get; set; }

    public string MaVaiTro { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
