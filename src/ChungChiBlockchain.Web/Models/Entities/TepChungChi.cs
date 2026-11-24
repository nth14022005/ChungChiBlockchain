using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class TepChungChi
{
    public int Id { get; set; }

    public int ChungChiId { get; set; }

    public string TenTep { get; set; } = null!;

    public string DuongDan { get; set; } = null!;

    public long? KichThuoc { get; set; }

    public string Sha256 { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ChungChi ChungChi { get; set; } = null!;
}
