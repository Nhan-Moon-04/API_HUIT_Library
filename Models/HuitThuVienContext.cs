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

    public virtual DbSet<ChucVu> ChucVus { get; set; }

    public virtual DbSet<DangKyPhong> DangKyPhongs { get; set; }

    public virtual DbSet<DanhGiaTv> DanhGiaTvs { get; set; }

    public virtual DbSet<GiangVien> GiangViens { get; set; }

    public virtual DbSet<LichHoatDongThuVien> LichHoatDongThuViens { get; set; }

    public virtual DbSet<LichTrangThaiPhong> LichTrangThaiPhongs { get; set; }

    public virtual DbSet<LichTruc> LichTrucs { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NhanVienThuVien> NhanVienThuViens { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<PhienChat> PhienChats { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<PhongTaiNguyen> PhongTaiNguyens { get; set; }

    public virtual DbSet<QuanLyKyThuat> QuanLyKyThuats { get; set; }

    public virtual DbSet<QuyDinhViPham> QuyDinhViPhams { get; set; }

    public virtual DbSet<SinhVien> SinhViens { get; set; }

    public virtual DbSet<SuDungPhong> SuDungPhongs { get; set; }

    public virtual DbSet<TaiNguyen> TaiNguyens { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TinNhan> TinNhans { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<ViPham> ViPhams { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=NGUYEN-NHAN\\SQLEXPRESS;Database=HUIT_ThuVien;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<ChucVu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__ChucVu__D46395338AFBF778");

            entity.ToTable("ChucVu");

            entity.HasIndex(e => e.TenChucVu, "UQ__ChucVu__A7E2123EB73EC5C1").IsUnique();

            entity.Property(e => e.TenChucVu).HasMaxLength(100);
        });

        modelBuilder.Entity<DangKyPhong>(entity =>
        {
            entity.HasKey(e => e.MaDangKy).HasName("PK__DangKyPh__BA90F02DB7AF823D");

            entity.ToTable("DangKyPhong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.LyDo).HasMaxLength(255);
            entity.Property(e => e.MaTrangThai).HasDefaultValue(1);
            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DangKyPhong_NguoiDung");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DangKyPhong_Phong");
        });

        modelBuilder.Entity<DanhGiaTv>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGiaT__AA9515BF747443F1");

            entity.ToTable("DanhGiaTV");

            entity.Property(e => e.LoaiDoiTuong).HasMaxLength(50);
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasMaxLength(500);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DanhGiaTvs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_NguoiDung");
        });

        modelBuilder.Entity<GiangVien>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__GiangVie__C539D762557531FB");

            entity.ToTable("GiangVien");

            entity.HasIndex(e => e.MaGiangVien, "UQ__GiangVie__C03BEEBB03D8D8CA").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.BoMon).HasMaxLength(100);
            entity.Property(e => e.HocHam).HasMaxLength(50);
            entity.Property(e => e.HocVi).HasMaxLength(50);
            entity.Property(e => e.Khoa).HasMaxLength(255);
            entity.Property(e => e.MaGiangVien)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.GiangVien)
                .HasForeignKey<GiangVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiangVien_NguoiDung");
        });

        modelBuilder.Entity<LichHoatDongThuVien>(entity =>
        {
            entity.HasKey(e => e.MaLichHoatDong).HasName("PK__LichHoat__FB597CDA54D20DC1");

            entity.ToTable("LichHoatDongThuVien");

            entity.Property(e => e.GhiChu).HasMaxLength(100);
            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.KhuVuc).HasMaxLength(50);
        });

        modelBuilder.Entity<LichTrangThaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLich).HasName("PK__LichTran__728A9AE90B8C687F");

            entity.ToTable("LichTrangThaiPhong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.LichTrangThaiPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichTrangThaiPhong_Phong");
        });

        modelBuilder.Entity<LichTruc>(entity =>
        {
            entity.HasKey(e => e.MaLichTruc).HasName("PK__LichTruc__9EB5133C033DD129");

            entity.ToTable("LichTruc");

            entity.Property(e => e.CaTruc).HasMaxLength(50);
            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.LichTrucs)
                .HasPrincipalKey(p => p.MaNhanVien)
                .HasForeignKey(d => d.MaNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichTruc_NhanVienThuVien");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__230212172606A230");

            entity.ToTable("LoaiPhong");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.SoLuongChoNgoi).HasMaxLength(255);
            entity.Property(e => e.TenLoaiPhong).HasMaxLength(100);
            entity.Property(e => e.ThoiGianSuDung).HasMaxLength(255);
            entity.Property(e => e.TrangThietBi).HasMaxLength(255);
            entity.Property(e => e.ViTri).HasMaxLength(100);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D762601315E9");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.MaDangNhap, "UQ__NguoiDun__C869B8C19163EA0A").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaDangNhap)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NguoiDung_VaiTro");
        });

        modelBuilder.Entity<NhanVienThuVien>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NhanVien__C539D7622280B682");

            entity.ToTable("NhanVienThuVien");

            entity.HasIndex(e => e.MaNhanVien, "UQ__NhanVien__77B2CA468769152C").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.NhanVienThuViens)
                .HasForeignKey(d => d.MaChucVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhanVienThuVien_ChucVu");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.NhanVienThuVien)
                .HasForeignKey<NhanVienThuVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhanVienThuVien_NguoiDung");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC079C789993");

            entity.ToTable("PasswordResetToken");

            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.Used).HasDefaultValue(false);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetToken_NguoiDung");
        });

        modelBuilder.Entity<PhienChat>(entity =>
        {
            entity.HasKey(e => e.MaPhienChat).HasName("PK__PhienCha__A91ECE76D7F2215C");

            entity.ToTable("PhienChat");

            entity.Property(e => e.CoBot).HasDefaultValue(false);
            entity.Property(e => e.ThoiGianBatDau)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.PhienChats)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhienChat_NguoiDung");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5BF1D7EFA3");

            entity.ToTable("Phong");

            entity.Property(e => e.MaTrangThai).HasDefaultValue(0);
            entity.Property(e => e.TenPhong).HasMaxLength(100);

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_LoaiPhong");
        });

        modelBuilder.Entity<PhongTaiNguyen>(entity =>
        {
            entity.HasKey(e => new { e.MaPhong, e.MaTaiNguyen }).HasName("PK__Phong_Ta__D680DD4615FFE5F9");

            entity.ToTable("Phong_TaiNguyen");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);
            entity.Property(e => e.TinhTrang)
                .HasMaxLength(50)
                .HasDefaultValue("Tốt");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.PhongTaiNguyens)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_TaiNguyen_Phong");

            entity.HasOne(d => d.MaTaiNguyenNavigation).WithMany(p => p.PhongTaiNguyens)
                .HasForeignKey(d => d.MaTaiNguyen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_TaiNguyen_TaiNguyen");
        });

        modelBuilder.Entity<QuanLyKyThuat>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__QuanLyKy__C539D762B8736E36");

            entity.ToTable("QuanLyKyThuat");

            entity.HasIndex(e => e.MaQuanTri, "UQ__QuanLyKy__05FA9349FB7202D4").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.MaQuanTri)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.QuanLyKyThuat)
                .HasForeignKey<QuanLyKyThuat>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuanLyKyThuat_NguoiDung");
        });

        modelBuilder.Entity<QuyDinhViPham>(entity =>
        {
            entity.HasKey(e => e.MaQuyDinh).HasName("PK__QuyDinhV__F7917049BAC4B304");

            entity.ToTable("QuyDinhViPham");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.HinhThucXuLy).HasMaxLength(255);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenViPham).HasMaxLength(255);
        });

        modelBuilder.Entity<SinhVien>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__SinhVien__C539D762791DC2F3");

            entity.ToTable("SinhVien");

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.Khoa).HasMaxLength(50);
            entity.Property(e => e.Lop).HasMaxLength(50);
            entity.Property(e => e.MaSinhVien)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NganhHoc).HasMaxLength(50);
            entity.Property(e => e.TrangThaiSinhVien).HasMaxLength(50);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.SinhVien)
                .HasForeignKey<SinhVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SinhVien_NguoiDung");
        });

        modelBuilder.Entity<SuDungPhong>(entity =>
        {
            entity.HasKey(e => e.MaSuDung).HasName("PK__SuDungPh__73EF96E99B283F6D");

            entity.ToTable("SuDungPhong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.GioBatDauThucTe).HasColumnType("datetime");
            entity.Property(e => e.GioKetThucThucTe).HasColumnType("datetime");
            entity.Property(e => e.TinhTrangPhong).HasMaxLength(255);

            entity.HasOne(d => d.MaDangKyNavigation).WithMany(p => p.SuDungPhongs)
                .HasForeignKey(d => d.MaDangKy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuDungPhong_DangKyPhong");
        });

        modelBuilder.Entity<TaiNguyen>(entity =>
        {
            entity.HasKey(e => e.MaTaiNguyen).HasName("PK__TaiNguye__63D831DE91C7A156");

            entity.ToTable("TaiNguyen");

            entity.Property(e => e.DonViTinh).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenTaiNguyen).HasMaxLength(100);
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54ED87210BE");

            entity.ToTable("ThongBao");

            entity.Property(e => e.DaDoc).HasDefaultValue(false);
            entity.Property(e => e.LoaiThongBao).HasMaxLength(50);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TieuDe).HasMaxLength(255);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThongBao_NguoiDung");
        });

        modelBuilder.Entity<TinNhan>(entity =>
        {
            entity.HasKey(e => e.MaTinNhan).HasName("PK__TinNhan__E5B3062AC43F755F");

            entity.ToTable("TinNhan");

            entity.Property(e => e.LaBot).HasDefaultValue(false);
            entity.Property(e => e.ThoiGianGui)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaPhienChatNavigation).WithMany(p => p.TinNhans)
                .HasForeignKey(d => d.MaPhienChat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TinNhan_PhienChat");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CFC6E52BD6");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.TenVaiTro, "UQ__VaiTro__1DA558142ECB013A").IsUnique();

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenVaiTro).HasMaxLength(50);
        });

        modelBuilder.Entity<ViPham>(entity =>
        {
            entity.HasKey(e => e.MaViPham).HasName("PK__ViPham__F1921D890405D886");

            entity.ToTable("ViPham");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThaiXuLy)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa xử lý");

            entity.HasOne(d => d.MaQuyDinhNavigation).WithMany(p => p.ViPhams)
                .HasForeignKey(d => d.MaQuyDinh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViPham_QuyDinhViPham");

            entity.HasOne(d => d.MaSuDungNavigation).WithMany(p => p.ViPhams)
                .HasForeignKey(d => d.MaSuDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViPham_SuDungPhong");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
