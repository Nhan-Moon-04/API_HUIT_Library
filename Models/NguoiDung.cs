using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class NguoiDung
{
    public int MaNguoiDung { get; set; }

    public string MaDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public int MaVaiTro { get; set; }

    public string? HoTen { get; set; }

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public virtual ICollection<DangKyPhong> DangKyPhongs { get; set; } = new List<DangKyPhong>();

    public virtual GiangVien? GiangVien { get; set; }

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;

    public virtual NhanVienThuVien? NhanVienThuVien { get; set; }

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual QuanLyKyThuat? QuanLyKyThuat { get; set; }

    public virtual SinhVien? SinhVien { get; set; }

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
}
