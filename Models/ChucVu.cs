using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class ChucVu
{
    public int MaChucVu { get; set; }

    public string? TenChucVu { get; set; }

    public virtual ICollection<NhanVienThuVien> NhanVienThuViens { get; set; } = new List<NhanVienThuVien>();
}
