using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class PhienBanHopDong
{
    public int Id { get; set; }

    public string TenMang { get; set; } = null!;

    public long ChainId { get; set; }

    public string DiaChiHopDong { get; set; } = null!;

    public string? TxTrienKhai { get; set; }

    public string PhienBan { get; set; } = null!;

    public bool DangSuDung { get; set; }

    public DateTime NgayTrienKhai { get; set; }

    public virtual ICollection<GiaoDichBlockchain> GiaoDichBlockchains { get; set; } = new List<GiaoDichBlockchain>();
}
