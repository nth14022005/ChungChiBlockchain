using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class MauChungChi
{
    public int Id { get; set; }

    public int ToChucId { get; set; }

    public int LoaiChungChiId { get; set; }

    public string TenMau { get; set; } = null!;

    public string? DuongDanMau { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();

    public virtual LoaiChungChi LoaiChungChi { get; set; } = null!;

    public virtual ToChuc ToChuc { get; set; } = null!;
}
