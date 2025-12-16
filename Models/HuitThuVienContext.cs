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

    public virtual DbSet<BotConversation> BotConversations { get; set; }

    public virtual DbSet<ChucVu> ChucVus { get; set; }

    public virtual DbSet<DangKyPhong> DangKyPhongs { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<GiangVien> GiangViens { get; set; }

    public virtual DbSet<LichDangKy> LichDangKies { get; set; }

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

    public virtual DbSet<TrangThaiDangKy> TrangThaiDangKies { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<ViPham> ViPhams { get; set; }

    public virtual DbSet<VisitLog> VisitLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=ADMIN-PC\\SQLEXPRESS;Database=HUIT_ThuVien;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<BotConversation>(entity =>
        {
            entity.ToTable("BotConversation");

            entity.HasIndex(e => e.ConversationId, "IX_BotConversation_ConversationId");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_BotConversation_UserId_IsActive");

            entity.Property(e => e.ConversationId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastUsedAt).HasColumnType("datetime");
            entity.Property(e => e.UserKey).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.BotConversations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BotConversation_NguoiDung");
        });

        modelBuilder.Entity<ChucVu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__ChucVu__D4639533656117D0");

            entity.ToTable("ChucVu");

            entity.HasIndex(e => e.TenChucVu, "UQ__ChucVu__A7E2123E043D77F7").IsUnique();

            entity.Property(e => e.TenChucVu).HasMaxLength(100);
        });

        modelBuilder.Entity<DangKyPhong>(entity =>
        {
            entity.HasKey(e => e.MaDangKy).HasName("PK__DangKyPh__BA90F02DACE43EC5");

            entity.ToTable("DangKyPhong", tb =>
                {
                    tb.HasTrigger("trg_KiemTraSoLuongNguoi");
                    tb.HasTrigger("trg_KiemTraThoiGianMuon");
                });

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.LyDo).HasMaxLength(255);
            entity.Property(e => e.MaPhong).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.MaTrangThai).HasDefaultValue(1);
            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.NgayMuon).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DangKyPhong_LoaiPhong");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DangKyPhong_NguoiDung");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK_DangKyPhong_Phong");

            entity.HasOne(d => d.MaTrangThaiNavigation).WithMany(p => p.DangKyPhongs)
                .HasForeignKey(d => d.MaTrangThai)
                .HasConstraintName("FK_DangKyPhong_TrangThai");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGia__AA9515BFCC870B25");

            entity.Property(e => e.NgayDanhGia)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NoiDung).HasMaxLength(500);

            entity.HasOne(d => d.MaDangKyNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaDangKy)
                .HasConstraintName("FK_DanhGia_DangKyPhong");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_DanhGia_NguoiDung");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_Phong");
        });

        modelBuilder.Entity<GiangVien>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__GiangVie__C539D762100E338B");

            entity.ToTable("GiangVien");

            entity.HasIndex(e => e.MaGiangVien, "UQ__GiangVie__C03BEEBB9BE4878E").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.BoMon).HasMaxLength(100);
            entity.Property(e => e.HocHam).HasMaxLength(50);
            entity.Property(e => e.HocVi).HasMaxLength(50);
            entity.Property(e => e.Khoa).HasMaxLength(100);
            entity.Property(e => e.MaGiangVien)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.GiangVien)
                .HasForeignKey<GiangVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiangVien_NguoiDung");
        });

        modelBuilder.Entity<LichDangKy>(entity =>
        {
            entity.HasKey(e => e.MaLich).HasName("PK__LichDang__728A9AE9642B7A1A");

            entity.ToTable("LichDangKy");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.LichDangKies)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichDangKy_Phong");
        });

        modelBuilder.Entity<LichHoatDongThuVien>(entity =>
        {
            entity.HasKey(e => e.MaLichHoatDong).HasName("PK__LichHoat__FB597CDAD7A0C518");

            entity.ToTable("LichHoatDongThuVien");

            entity.Property(e => e.GhiChu).HasMaxLength(100);
            entity.Property(e => e.HoatDong).HasDefaultValue(true);
        });

        modelBuilder.Entity<LichTrangThaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLich).HasName("PK__LichTran__728A9AE917A5B238");

            entity.ToTable("LichTrangThaiPhong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Trống");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.LichTrangThaiPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichTrangThaiPhong_Phong");
        });

        modelBuilder.Entity<LichTruc>(entity =>
        {
            entity.HasKey(e => e.MaLichTruc).HasName("PK__LichTruc__9EB5133C00ABF738");

            entity.ToTable("LichTruc");

            entity.Property(e => e.CaTruc).HasMaxLength(50);
            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.LichTrucs)
                .HasPrincipalKey(p => p.MaNhanVien)
                .HasForeignKey(d => d.MaNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichTruc_NhanVienThuVien");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__23021217CAA851ED");

            entity.ToTable("LoaiPhong");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenLoaiPhong).HasMaxLength(100);
            entity.Property(e => e.ThoiGianSuDung).HasMaxLength(255);
            entity.Property(e => e.TrangThietBi).HasMaxLength(255);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D762B55E8EDD");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.MaDangNhap, "UQ__NguoiDun__C869B8C133160C23").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.LastActivity).HasColumnType("datetime");
            entity.Property(e => e.MaDangNhap)
                .HasMaxLength(20)
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
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NhanVien__C539D7624290D339");

            entity.ToTable("NhanVienThuVien");

            entity.HasIndex(e => e.MaNhanVien, "UQ__NhanVien__77B2CA46A9AEB883").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.NhanVienThuViens)
                .HasForeignKey(d => d.MaChucVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NVTV_ChucVu");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.NhanVienThuVien)
                .HasForeignKey<NhanVienThuVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NVTV_NguoiDung");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC077AB81A68");

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
            entity.HasKey(e => e.MaPhienChat).HasName("PK__PhienCha__A91ECE76ABAF56C8");

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
            entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5B3C891F73");

            entity.ToTable("Phong");

            entity.Property(e => e.TenPhong).HasMaxLength(100);
            entity.Property(e => e.TinhTrang).HasMaxLength(20);

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_LoaiPhong");
        });

        modelBuilder.Entity<PhongTaiNguyen>(entity =>
        {
            entity.HasKey(e => new { e.MaPhong, e.MaTaiNguyen }).HasName("PK__Phong_Ta__D680DD46D5F18851");

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
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__QuanLyKy__C539D762A59F2E9A");

            entity.ToTable("QuanLyKyThuat");

            entity.HasIndex(e => e.MaQuanTri, "UQ__QuanLyKy__05FA9349F59E1876").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.MaQuanTri)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.QuanLyKyThuat)
                .HasForeignKey<QuanLyKyThuat>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QLKT_NguoiDung");
        });

        modelBuilder.Entity<QuyDinhViPham>(entity =>
        {
            entity.HasKey(e => e.MaQuyDinh).HasName("PK__QuyDinhV__F791704903F68EE0");

            entity.ToTable("QuyDinhViPham");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.HinhThucXuLy).HasMaxLength(255);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenViPham).HasMaxLength(255);
        });

        modelBuilder.Entity<SinhVien>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__SinhVien__C539D762D3913CC2");

            entity.ToTable("SinhVien");

            entity.HasIndex(e => e.MaSinhVien, "UQ__SinhVien__939AE77485E30A7C").IsUnique();

            entity.Property(e => e.MaNguoiDung).ValueGeneratedNever();
            entity.Property(e => e.Khoa).HasMaxLength(50);
            entity.Property(e => e.Lop).HasMaxLength(50);
            entity.Property(e => e.MaSinhVien)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NganhHoc).HasMaxLength(50);
            entity.Property(e => e.TrangThaiSinhVien).HasMaxLength(50);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithOne(p => p.SinhVien)
                .HasForeignKey<SinhVien>(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SinhVien_NguoiDung");
        });

        modelBuilder.Entity<SuDungPhong>(entity =>
        {
            entity.HasKey(e => e.MaSuDung).HasName("PK__SuDungPh__73EF96E9512B216D");

            entity.ToTable("SuDungPhong");

            entity.HasIndex(e => e.MaDangKy, "UQ__SuDungPh__BA90F02CAA23C66A").IsUnique();

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.GioBatDauThucTe).HasColumnType("datetime");
            entity.Property(e => e.GioKetThucThucTe).HasColumnType("datetime");
            entity.Property(e => e.TinhTrangPhong).HasMaxLength(255);

            entity.HasOne(d => d.MaDangKyNavigation).WithOne(p => p.SuDungPhong)
                .HasForeignKey<SuDungPhong>(d => d.MaDangKy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuDungPhong_DangKyPhong");
        });

        modelBuilder.Entity<TaiNguyen>(entity =>
        {
            entity.HasKey(e => e.MaTaiNguyen).HasName("PK__TaiNguye__63D831DEB1F9AB97");

            entity.ToTable("TaiNguyen");

            entity.Property(e => e.ChuaSuDung).HasDefaultValue(0);
            entity.Property(e => e.DangSuDung).HasDefaultValue(0);
            entity.Property(e => e.DonViTinh).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenTaiNguyen).HasMaxLength(100);
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54EE5CA8783");

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
            entity.HasKey(e => e.MaTinNhan).HasName("PK__TinNhan__E5B3062A5D46D630");

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

        modelBuilder.Entity<TrangThaiDangKy>(entity =>
        {
            entity.HasKey(e => e.MaTrangThai).HasName("PK__TrangTha__AADE413831F13E86");

            entity.ToTable("TrangThaiDangKy");

            entity.HasIndex(e => e.TenTrangThai, "UQ__TrangTha__9489EF66482BA5EC").IsUnique();

            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CF7C0FCF3B");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.TenVaiTro, "UQ__VaiTro__1DA55814E0EFC446").IsUnique();

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenVaiTro).HasMaxLength(50);
        });

        modelBuilder.Entity<ViPham>(entity =>
        {
            entity.HasKey(e => e.MaViPham).HasName("PK__ViPham__F1921D89A2459904");

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
                .HasConstraintName("FK_ViPham_QuyDinhViPham");

            entity.HasOne(d => d.MaSuDungNavigation).WithMany(p => p.ViPhams)
                .HasForeignKey(d => d.MaSuDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViPham_SuDungPhong");
        });

        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VisitLog__3214EC07B159B62F");

            entity.ToTable("VisitLog");

            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.VisitTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_VisitLog_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
