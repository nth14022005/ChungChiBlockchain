using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class LoaiChungChi
{
    public int Id { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<MauChungChi> MauChungChis { get; set; } = new List<MauChungChi>();
}
