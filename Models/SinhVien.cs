using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class SinhVien
{
    public int MaNguoiDung { get; set; }

    public string? MaSinhVien { get; set; }

    public string? Lop { get; set; }

    public string? NganhHoc { get; set; }

    public string? Khoa { get; set; }

    public string? TrangThaiSinhVien { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
