using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class NguoiHoc
{
    public int Id { get; set; }

    public int ToChucId { get; set; }

    public string MaNguoiHoc { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();

    public virtual ToChuc ToChuc { get; set; } = null!;
}
