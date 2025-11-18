using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class DanhGium
{
    public int MaDanhGia { get; set; }

    public int MaNguoiDung { get; set; }

    public int MaDangKy { get; set; }

    public int MaPhong { get; set; }

    public byte DiemDanhGia { get; set; }

    public string? NoiDung { get; set; }

    public DateTime NgayDanhGia { get; set; }

    public virtual DangKyPhong MaDangKyNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}
