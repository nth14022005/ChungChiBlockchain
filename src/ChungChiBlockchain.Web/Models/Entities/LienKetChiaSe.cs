using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class LienKetChiaSe
{
    public int Id { get; set; }

    public int ChungChiId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime? HetHan { get; set; }

    public int SoLanXem { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChungChi ChungChi { get; set; } = null!;
}
