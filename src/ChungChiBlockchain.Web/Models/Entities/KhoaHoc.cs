using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class KhoaHoc
{
    public int Id { get; set; }

    public int ToChucId { get; set; }

    public string MaKhoaHoc { get; set; } = null!;

    public string TenKhoaHoc { get; set; } = null!;

    public string? ThoiLuong { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();

    public virtual ToChuc ToChuc { get; set; } = null!;
}
