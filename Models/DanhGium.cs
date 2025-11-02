using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class DanhGium
{
    public int MaDanhGia { get; set; }

    public int MaNguoiDung { get; set; }

    public string LoaiDoiTuong { get; set; } = null!;

    public int MaDoiTuong { get; set; }

    public int? DiemDanhGia { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
