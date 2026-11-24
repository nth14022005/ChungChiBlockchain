using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class ViBlockchain
{
    public int Id { get; set; }

    public int ToChucId { get; set; }

    public string DiaChiVi { get; set; } = null!;

    public string? Nhan { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayThem { get; set; }

    public virtual ICollection<GiaoDichBlockchain> GiaoDichBlockchains { get; set; } = new List<GiaoDichBlockchain>();

    public virtual ToChuc ToChuc { get; set; } = null!;
}
