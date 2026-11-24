using System;
using System.Collections.Generic;

namespace ChungChiBlockchain.Web.Models.Entities;

public partial class ThuHoiChungChi
{
    public int Id { get; set; }

    public int ChungChiId { get; set; }

    public int NguoiThuHoiId { get; set; }

    public int? GiaoDichId { get; set; }

    public string LyDo { get; set; } = null!;

    public DateTime NgayThuHoi { get; set; }

    public virtual ChungChi ChungChi { get; set; } = null!;

    public virtual GiaoDichBlockchain? GiaoDich { get; set; }

    public virtual NguoiDung NguoiThuHoi { get; set; } = null!;
}
