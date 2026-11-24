using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class LoPhatHanh
{
    public int Id { get; set; }

    public int ToChucId { get; set; }

    public int NguoiTaoId { get; set; }

    public string TenLo { get; set; } = null!;

    public int SoLuong { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();

    public virtual NguoiDung NguoiTao { get; set; } = null!;

    public virtual ToChuc ToChuc { get; set; } = null!;
}
