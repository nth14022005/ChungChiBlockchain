using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class NguoiDung
{
    public int Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhauHash { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? Email { get; set; }

    public int VaiTroId { get; set; }

    public int? ToChucId { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();

    public virtual DoanhNghiepXacMinh? DoanhNghiepXacMinh { get; set; }

    public virtual ICollection<LoPhatHanh> LoPhatHanhs { get; set; } = new List<LoPhatHanh>();

    public virtual ICollection<NhatKyHoatDong> NhatKyHoatDongs { get; set; } = new List<NhatKyHoatDong>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<ThuHoiChungChi> ThuHoiChungChis { get; set; } = new List<ThuHoiChungChi>();

    public virtual ToChuc? ToChuc { get; set; }

    public virtual VaiTro VaiTro { get; set; } = null!;
}
