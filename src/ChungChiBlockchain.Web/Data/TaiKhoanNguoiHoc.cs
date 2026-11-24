using ChungChiBlockchain.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Data;

public sealed class TaiKhoanNguoiHoc
{
    public int NguoiDungId { get; set; }
    public int NguoiHocId { get; set; }
    public NguoiDung NguoiDung { get; set; } = null!;
}

public partial class AppDbContext
{
    public DbSet<TaiKhoanNguoiHoc> TaiKhoanNguoiHocs => Set<TaiKhoanNguoiHoc>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<TaiKhoanNguoiHoc>();
        link.ToTable("tai_khoan_nguoi_hoc");
        link.HasKey(x => x.NguoiDungId);
        link.Property(x => x.NguoiDungId).HasColumnName("nguoi_dung_id").ValueGeneratedNever();
        link.Property(x => x.NguoiHocId).HasColumnName("nguoi_hoc_id");
        link.HasIndex(x => x.NguoiHocId).IsUnique();
        link.HasOne(x => x.NguoiDung).WithOne().HasForeignKey<TaiKhoanNguoiHoc>(x => x.NguoiDungId)
            .OnDelete(DeleteBehavior.Restrict);
        link.HasOne<NguoiHoc>().WithOne().HasForeignKey<TaiKhoanNguoiHoc>(x => x.NguoiHocId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
