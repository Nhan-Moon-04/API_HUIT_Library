using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class NguoiDung
{
    public int MaNguoiDung { get; set; }

    public string MaCode { get; set; } = null!;

    public string? MaSinhVien { get; set; }

    public string? MaNhanVien { get; set; }

    public string HoTen { get; set; } = null!;

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public string? MatKhau { get; set; }

    public int MaVaiTro { get; set; }

    public string? Lop { get; set; }

    public string? KhoaHoc { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<DatPhong> DatPhongMaNguoiDungNavigations { get; set; } = new List<DatPhong>();

    public virtual ICollection<DatPhong> DatPhongNguoiDuyetNavigations { get; set; } = new List<DatPhong>();

    public virtual ICollection<DatPhong> DatPhongNguoiHuyNavigations { get; set; } = new List<DatPhong>();

    public virtual ICollection<DatPhong> DatPhongNguoiTaoNavigations { get; set; } = new List<DatPhong>();

    public virtual VaiTroNguoiDung MaVaiTroNavigation { get; set; } = null!;

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
}
