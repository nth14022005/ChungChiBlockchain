using System;
using System.Collections.Generic;
using ChungChiBlockchain.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChungChi> ChungChis { get; set; }

    public virtual DbSet<DoanhNghiepXacMinh> DoanhNghiepXacMinhs { get; set; }

    public virtual DbSet<GiaoDichBlockchain> GiaoDichBlockchains { get; set; }

    public virtual DbSet<KhoaHoc> KhoaHocs { get; set; }

    public virtual DbSet<LichSuXacMinh> LichSuXacMinhs { get; set; }

    public virtual DbSet<LienKetChiaSe> LienKetChiaSes { get; set; }

    public virtual DbSet<LoPhatHanh> LoPhatHanhs { get; set; }

    public virtual DbSet<LoaiChungChi> LoaiChungChis { get; set; }

    public virtual DbSet<MauChungChi> MauChungChis { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NguoiHoc> NguoiHocs { get; set; }

    public virtual DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }

    public virtual DbSet<PhienBanHopDong> PhienBanHopDongs { get; set; }

    public virtual DbSet<TepChungChi> TepChungChis { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<ThuHoiChungChi> ThuHoiChungChis { get; set; }

    public virtual DbSet<ToChuc> ToChucs { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<ViBlockchain> ViBlockchains { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChungChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chung_ch__3213E83F9A8509E4");

            entity.ToTable("chung_chi");

            entity.HasIndex(e => e.MaChungChi, "UQ__chung_ch__483A1369C833C010").IsUnique();

            entity.HasIndex(e => e.NguoiHocId, "ix_chungchi_nguoihoc");

            entity.HasIndex(e => e.TrangThai, "ix_chungchi_trangthai");

            entity.HasIndex(e => e.HashChungChi, "ux_chungchi_hash")
                .IsUnique()
                .HasFilter("([hash_chung_chi] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DuLieuChungChi).HasColumnName("du_lieu_chung_chi");
            entity.Property(e => e.HashChungChi)
                .HasMaxLength(66)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("hash_chung_chi");
            entity.Property(e => e.KhoaHocId).HasColumnName("khoa_hoc_id");
            entity.Property(e => e.LoPhatHanhId).HasColumnName("lo_phat_hanh_id");
            entity.Property(e => e.MaChungChi)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("ma_chung_chi");
            entity.Property(e => e.MauChungChiId).HasColumnName("mau_chung_chi_id");
            entity.Property(e => e.NgayCap).HasColumnName("ngay_cap");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiCapId).HasColumnName("nguoi_cap_id");
            entity.Property(e => e.NguoiHocId).HasColumnName("nguoi_hoc_id");
            entity.Property(e => e.PhienBanBam)
                .HasDefaultValue((byte)1)
                .HasColumnName("phien_ban_bam");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("nhap")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XepLoai)
                .HasMaxLength(50)
                .HasColumnName("xep_loai");

            entity.HasOne(d => d.KhoaHoc).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.KhoaHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chung_chi__khoa___6C190EBB");

            entity.HasOne(d => d.LoPhatHanh).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.LoPhatHanhId)
                .HasConstraintName("FK__chung_chi__lo_ph__6E01572D");

            entity.HasOne(d => d.MauChungChi).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.MauChungChiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chung_chi__mau_c__6D0D32F4");

            entity.HasOne(d => d.NguoiCap).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.NguoiCapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chung_chi__nguoi__6EF57B66");

            entity.HasOne(d => d.NguoiHoc).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.NguoiHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__chung_chi__nguoi__6B24EA82");
        });

        modelBuilder.Entity<DoanhNghiepXacMinh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__doanh_ng__3213E83FF5A3858D");

            entity.ToTable("doanh_nghiep_xac_minh");

            entity.HasIndex(e => e.NguoiDungId, "uq_dn_nguoidung").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaSoThue)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ma_so_thue");
            entity.Property(e => e.NgayDangKy)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_dang_ky");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NguoiLienHe)
                .HasMaxLength(100)
                .HasColumnName("nguoi_lien_he");
            entity.Property(e => e.TenDoanhNghiep)
                .HasMaxLength(200)
                .HasColumnName("ten_doanh_nghiep");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.DoanhNghiepXacMinh)
                .HasForeignKey<DoanhNghiepXacMinh>(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__doanh_ngh__nguoi__0A9D95DB");
        });

        modelBuilder.Entity<GiaoDichBlockchain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__giao_dic__3213E83F4CE04177");

            entity.ToTable("giao_dich_blockchain");

            entity.HasIndex(e => new { e.TrangThai, e.NgayGui }, "ix_giaodich_trangthai");

            entity.HasIndex(e => e.TxHash, "ux_giaodich_tx")
                .IsUnique()
                .HasFilter("([tx_hash] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungChiId).HasColumnName("chung_chi_id");
            entity.Property(e => e.HopDongId).HasColumnName("hop_dong_id");
            entity.Property(e => e.Loai)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("loai");
            entity.Property(e => e.NgayGui)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_gui");
            entity.Property(e => e.NgayXacNhan)
                .HasColumnType("datetime")
                .HasColumnName("ngay_xac_nhan");
            entity.Property(e => e.SoKhoi).HasColumnName("so_khoi");
            entity.Property(e => e.ThongBaoLoi)
                .HasMaxLength(500)
                .HasColumnName("thong_bao_loi");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("trang_thai");
            entity.Property(e => e.TxHash)
                .HasMaxLength(66)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tx_hash");
            entity.Property(e => e.ViId).HasColumnName("vi_id");

            entity.HasOne(d => d.ChungChi).WithMany(p => p.GiaoDichBlockchains)
                .HasForeignKey(d => d.ChungChiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__giao_dich__chung__7A672E12");

            entity.HasOne(d => d.HopDong).WithMany(p => p.GiaoDichBlockchains)
                .HasForeignKey(d => d.HopDongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__giao_dich__hop_d__7B5B524B");

            entity.HasOne(d => d.Vi).WithMany(p => p.GiaoDichBlockchains)
                .HasForeignKey(d => d.ViId)
                .HasConstraintName("FK__giao_dich__vi_id__7C4F7684");
        });

        modelBuilder.Entity<KhoaHoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__khoa_hoc__3213E83F155390E1");

            entity.ToTable("khoa_hoc");

            entity.HasIndex(e => new { e.ToChucId, e.MaKhoaHoc }, "uq_khoahoc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaKhoaHoc)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ma_khoa_hoc");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenKhoaHoc)
                .HasMaxLength(200)
                .HasColumnName("ten_khoa_hoc");
            entity.Property(e => e.ThoiLuong)
                .HasMaxLength(50)
                .HasColumnName("thoi_luong");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.KhoaHocs)
                .HasForeignKey(d => d.ToChucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__khoa_hoc__to_chu__534D60F1");
        });

        modelBuilder.Entity<LichSuXacMinh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lich_su___3213E83F792BBC48");

            entity.ToTable("lich_su_xac_minh");

            entity.HasIndex(e => e.ThoiGian, "ix_xacminh_thoigian");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungChiId).HasColumnName("chung_chi_id");
            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("dia_chi_ip");
            entity.Property(e => e.DoanhNghiepId).HasColumnName("doanh_nghiep_id");
            entity.Property(e => e.HashTraCuu)
                .HasMaxLength(66)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("hash_tra_cuu");
            entity.Property(e => e.KetQua)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ket_qua");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("thoi_gian");

            entity.HasOne(d => d.ChungChi).WithMany(p => p.LichSuXacMinhs)
                .HasForeignKey(d => d.ChungChiId)
                .HasConstraintName("FK__lich_su_x__chung__0E6E26BF");

            entity.HasOne(d => d.DoanhNghiep).WithMany(p => p.LichSuXacMinhs)
                .HasForeignKey(d => d.DoanhNghiepId)
                .HasConstraintName("FK__lich_su_x__doanh__0F624AF8");
        });

        modelBuilder.Entity<LienKetChiaSe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lien_ket__3213E83FAE6CE3CE");

            entity.ToTable("lien_ket_chia_se");

            entity.HasIndex(e => e.Token, "UQ__lien_ket__CA90DA7A103810D1").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungChiId).HasColumnName("chung_chi_id");
            entity.Property(e => e.HetHan)
                .HasColumnType("datetime")
                .HasColumnName("het_han");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoLanXem).HasColumnName("so_lan_xem");
            entity.Property(e => e.Token)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("token");

            entity.HasOne(d => d.ChungChi).WithMany(p => p.LienKetChiaSes)
                .HasForeignKey(d => d.ChungChiId)
                .HasConstraintName("FK__lien_ket___chung__160F4887");
        });

        modelBuilder.Entity<LoPhatHanh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lo_phat___3213E83F6C711D49");

            entity.ToTable("lo_phat_hanh");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.TenLo)
                .HasMaxLength(150)
                .HasColumnName("ten_lo");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("nhap")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.LoPhatHanhs)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__lo_phat_h__nguoi__6383C8BA");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.LoPhatHanhs)
                .HasForeignKey(d => d.ToChucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__lo_phat_h__to_ch__628FA481");
        });

        modelBuilder.Entity<LoaiChungChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__loai_chu__3213E83FA5E25A22");

            entity.ToTable("loai_chung_chi");

            entity.HasIndex(e => e.TenLoai, "UQ__loai_chu__5FFB310CD9374197").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenLoai)
                .HasMaxLength(100)
                .HasColumnName("ten_loai");
        });

        modelBuilder.Entity<MauChungChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__mau_chun__3213E83F2CA28A15");

            entity.ToTable("mau_chung_chi");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DuongDanMau)
                .HasMaxLength(500)
                .HasColumnName("duong_dan_mau");
            entity.Property(e => e.LoaiChungChiId).HasColumnName("loai_chung_chi_id");
            entity.Property(e => e.TenMau)
                .HasMaxLength(150)
                .HasColumnName("ten_mau");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.LoaiChungChi).WithMany(p => p.MauChungChis)
                .HasForeignKey(d => d.LoaiChungChiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mau_chung__loai___59FA5E80");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.MauChungChis)
                .HasForeignKey(d => d.ToChucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mau_chung__to_ch__59063A47");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nguoi_du__3213E83F76CDDE14");

            entity.ToTable("nguoi_dung");

            entity.HasIndex(e => e.TenDangNhap, "UQ__nguoi_du__363698B36888A59B").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.MatKhauHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("mat_khau_hash");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ten_dang_nhap");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true)
                .HasColumnName("trang_thai");
            entity.Property(e => e.VaiTroId).HasColumnName("vai_tro_id");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.ToChucId)
                .HasConstraintName("FK__nguoi_dun__to_ch__4222D4EF");

            entity.HasOne(d => d.VaiTro).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.VaiTroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__nguoi_dun__vai_t__412EB0B6");
        });

        modelBuilder.Entity<NguoiHoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nguoi_ho__3213E83FBF7DE1D2");

            entity.ToTable("nguoi_hoc");

            entity.HasIndex(e => new { e.ToChucId, e.MaNguoiHoc }, "uq_nguoihoc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.MaNguoiHoc)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ma_nguoi_hoc");
            entity.Property(e => e.NgaySinh).HasColumnName("ngay_sinh");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.NguoiHocs)
                .HasForeignKey(d => d.ToChucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__nguoi_hoc__to_ch__5EBF139D");
        });

        modelBuilder.Entity<NhatKyHoatDong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nhat_ky___3213E83FE25327B9");

            entity.ToTable("nhat_ky_hoat_dong");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChiTiet)
                .HasMaxLength(500)
                .HasColumnName("chi_tiet");
            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("dia_chi_ip");
            entity.Property(e => e.DoiTuong)
                .HasMaxLength(100)
                .HasColumnName("doi_tuong");
            entity.Property(e => e.HanhDong)
                .HasMaxLength(100)
                .HasColumnName("hanh_dong");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("thoi_gian");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NhatKyHoatDongs)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK__nhat_ky_h__nguoi__1F98B2C1");
        });

        modelBuilder.Entity<PhienBanHopDong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__phien_ba__3213E83F7D020EFA");

            entity.ToTable("phien_ban_hop_dong");

            entity.HasIndex(e => new { e.ChainId, e.DiaChiHopDong }, "uq_hopdong").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChainId).HasColumnName("chain_id");
            entity.Property(e => e.DangSuDung).HasColumnName("dang_su_dung");
            entity.Property(e => e.DiaChiHopDong)
                .HasMaxLength(42)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("dia_chi_hop_dong");
            entity.Property(e => e.NgayTrienKhai)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_trien_khai");
            entity.Property(e => e.PhienBan)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phien_ban");
            entity.Property(e => e.TenMang)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ten_mang");
            entity.Property(e => e.TxTrienKhai)
                .HasMaxLength(66)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tx_trien_khai");
        });

        modelBuilder.Entity<TepChungChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tep_chun__3213E83FD669B4CE");

            entity.ToTable("tep_chung_chi");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungChiId).HasColumnName("chung_chi_id");
            entity.Property(e => e.DuongDan)
                .HasMaxLength(500)
                .HasColumnName("duong_dan");
            entity.Property(e => e.KichThuoc).HasColumnName("kich_thuoc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.Sha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("sha256");
            entity.Property(e => e.TenTep)
                .HasMaxLength(200)
                .HasColumnName("ten_tep");

            entity.HasOne(d => d.ChungChi).WithMany(p => p.TepChungChis)
                .HasForeignKey(d => d.ChungChiId)
                .HasConstraintName("FK__tep_chung__chung__76969D2E");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thong_ba__3213E83F874040AA");

            entity.ToTable("thong_bao");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DaDoc).HasColumnName("da_doc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NoiDung)
                .HasMaxLength(1000)
                .HasColumnName("noi_dung");
            entity.Property(e => e.TieuDe)
                .HasMaxLength(200)
                .HasColumnName("tieu_de");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK__thong_bao__nguoi__1AD3FDA4");
        });

        modelBuilder.Entity<ThuHoiChungChi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__thu_hoi___3213E83FF4B38976");

            entity.ToTable("thu_hoi_chung_chi");

            entity.HasIndex(e => e.ChungChiId, "uq_thuhoi_chungchi").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungChiId).HasColumnName("chung_chi_id");
            entity.Property(e => e.GiaoDichId).HasColumnName("giao_dich_id");
            entity.Property(e => e.LyDo)
                .HasMaxLength(500)
                .HasColumnName("ly_do");
            entity.Property(e => e.NgayThuHoi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_thu_hoi");
            entity.Property(e => e.NguoiThuHoiId).HasColumnName("nguoi_thu_hoi_id");

            entity.HasOne(d => d.ChungChi).WithOne(p => p.ThuHoiChungChi)
                .HasForeignKey<ThuHoiChungChi>(d => d.ChungChiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__thu_hoi_c__chung__03F0984C");

            entity.HasOne(d => d.GiaoDich).WithMany(p => p.ThuHoiChungChis)
                .HasForeignKey(d => d.GiaoDichId)
                .HasConstraintName("FK__thu_hoi_c__giao___05D8E0BE");

            entity.HasOne(d => d.NguoiThuHoi).WithMany(p => p.ThuHoiChungChis)
                .HasForeignKey(d => d.NguoiThuHoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__thu_hoi_c__nguoi__04E4BC85");
        });

        modelBuilder.Entity<ToChuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__to_chuc__3213E83F991619F0");

            entity.ToTable("to_chuc");

            entity.HasIndex(e => e.MaToChuc, "UQ__to_chuc__4A01BE07DAB515F8").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(300)
                .HasColumnName("dia_chi");
            entity.Property(e => e.EmailLienHe)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email_lien_he");
            entity.Property(e => e.MaToChuc)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ma_to_chuc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.TenToChuc)
                .HasMaxLength(200)
                .HasColumnName("ten_to_chuc");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("hoat_dong")
                .HasColumnName("trang_thai");
            entity.Property(e => e.Website)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("website");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vai_tro__3213E83F9D594DCE");

            entity.ToTable("vai_tro");

            entity.HasIndex(e => e.MaVaiTro, "UQ__vai_tro__4AE1754CA61623B3").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaVaiTro)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ma_vai_tro");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenVaiTro)
                .HasMaxLength(100)
                .HasColumnName("ten_vai_tro");
        });

        modelBuilder.Entity<ViBlockchain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vi_block__3213E83F412C3826");

            entity.ToTable("vi_blockchain");

            entity.HasIndex(e => e.DiaChiVi, "UQ__vi_block__F01612A8C5FEAFE3").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChiVi)
                .HasMaxLength(42)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("dia_chi_vi");
            entity.Property(e => e.NgayThem)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngay_them");
            entity.Property(e => e.Nhan)
                .HasMaxLength(100)
                .HasColumnName("nhan");
            entity.Property(e => e.ToChucId).HasColumnName("to_chuc_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("cho_duyet")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.ToChuc).WithMany(p => p.ViBlockchains)
                .HasForeignKey(d => d.ToChucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__vi_blockc__to_ch__47DBAE45");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
