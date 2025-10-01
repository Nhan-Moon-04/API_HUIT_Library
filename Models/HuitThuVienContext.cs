using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HUIT_Library.Models;

public partial class HuitThuVienContext : DbContext
{
    public HuitThuVienContext()
    {
    }

    public HuitThuVienContext(DbContextOptions<HuitThuVienContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DatPhong> DatPhongs { get; set; }

    public virtual DbSet<LichThuVien> LichThuViens { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TrangThaiDat> TrangThaiDats { get; set; }

    public virtual DbSet<VaiTroNguoiDung> VaiTroNguoiDungs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<DatPhong>(entity =>
        {
            entity.HasKey(e => e.MaDat).HasName("PK__DatPhong__3D8883305DCABAE4");

            entity.ToTable("DatPhong");

            entity.HasIndex(e => e.MaCode, "UQ__DatPhong__152C7C5CD2127FDD").IsUnique();

            entity.Property(e => e.GhiChuDuyet).HasMaxLength(500);
            entity.Property(e => e.LyDoHuy).HasMaxLength(500);
            entity.Property(e => e.MaCode).HasMaxLength(20);
            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("CHO_DUYET");
            entity.Property(e => e.MoTaHuHong).HasMaxLength(1000);
            entity.Property(e => e.MucDich).HasMaxLength(500);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PhanHoi).HasMaxLength(1000);
            entity.Property(e => e.TinhTrangSau).HasMaxLength(50);
            entity.Property(e => e.YeuCauDacBiet).HasMaxLength(1000);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DatPhongMaNguoiDungNavigations)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_NguoiDung");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_Phong");

            entity.HasOne(d => d.MaTrangThaiNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaTrangThai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_TrangThai");

            entity.HasOne(d => d.NguoiDuyetNavigation).WithMany(p => p.DatPhongNguoiDuyetNavigations)
                .HasForeignKey(d => d.NguoiDuyet)
                .HasConstraintName("FK_DatPhong_Duyet");

            entity.HasOne(d => d.NguoiHuyNavigation).WithMany(p => p.DatPhongNguoiHuyNavigations)
                .HasForeignKey(d => d.NguoiHuy)
                .HasConstraintName("FK_DatPhong_Huy");

            entity.HasOne(d => d.NguoiTaoNavigation).WithMany(p => p.DatPhongNguoiTaoNavigations)
                .HasForeignKey(d => d.NguoiTao)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_Tao");
        });

        modelBuilder.Entity<LichThuVien>(entity =>
        {
            entity.HasKey(e => e.MaLich).HasName("PK__LichThuV__728A9AE945957B18");

            entity.ToTable("LichThuVien");

            entity.Property(e => e.CoMoCua).HasDefaultValue(true);
            entity.Property(e => e.MoTa).HasMaxLength(200);
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoai).HasName("PK__LoaiPhon__730A575985A20F8C");

            entity.ToTable("LoaiPhong");

            entity.HasIndex(e => e.MaCode, "UQ__LoaiPhon__152C7C5CC8B82F87").IsUnique();

            entity.Property(e => e.MaCode).HasMaxLength(20);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenLoai).HasMaxLength(200);
            entity.Property(e => e.ThuTu).HasDefaultValue(0);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D762586472CA");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.MaNhanVien, "UQ_NguoiDung_NhanVien")
                .IsUnique()
                .HasFilter("([MaNhanVien] IS NOT NULL)");

            entity.HasIndex(e => e.MaSinhVien, "UQ_NguoiDung_SinhVien")
                .IsUnique()
                .HasFilter("([MaSinhVien] IS NOT NULL)");

            entity.HasIndex(e => e.MaCode, "UQ__NguoiDun__152C7C5CB7DAB339").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D105341B7D63B7").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.HoTen).HasMaxLength(200);
            entity.Property(e => e.KhoaHoc).HasMaxLength(10);
            entity.Property(e => e.Lop).HasMaxLength(50);
            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.MaNhanVien).HasMaxLength(20);
            entity.Property(e => e.MaSinhVien).HasMaxLength(20);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.SoDienThoai).HasMaxLength(15);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NguoiDung_VaiTro");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5BD1F51C7E");

            entity.ToTable("Phong");

            entity.HasIndex(e => e.MaCode, "UQ__Phong__152C7C5CD6CFABC0").IsUnique();

            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenPhong).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("SAN_SANG");
            entity.Property(e => e.ViTri).HasMaxLength(200);

            entity.HasOne(d => d.MaLoaiNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_Loai");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54ECF8CC144");

            entity.ToTable("ThongBao");

            entity.Property(e => e.Loai)
                .HasMaxLength(50)
                .HasDefaultValue("NHAC_NHO");
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NoiDung).HasMaxLength(1000);
            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.MaDatNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaDat)
                .HasConstraintName("FK_ThongBao_DatPhong");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThongBao_NguoiDung");
        });

        modelBuilder.Entity<TrangThaiDat>(entity =>
        {
            entity.HasKey(e => e.MaCode).HasName("PK__TrangTha__152C7C5D97AC264A");

            entity.ToTable("TrangThaiDat");

            entity.Property(e => e.MaCode).HasMaxLength(20);
            entity.Property(e => e.Mau).HasMaxLength(7);
            entity.Property(e => e.MoTa).HasMaxLength(200);
            entity.Property(e => e.TenTrangThai).HasMaxLength(100);
            entity.Property(e => e.ThuTu).HasDefaultValue(0);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<VaiTroNguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTroNg__C24C41CF1C87B9B6");

            entity.ToTable("VaiTroNguoiDung");

            entity.HasIndex(e => e.MaCode, "UQ__VaiTroNg__152C7C5C54B28C3D").IsUnique();

            entity.Property(e => e.MaCode).HasMaxLength(20);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
