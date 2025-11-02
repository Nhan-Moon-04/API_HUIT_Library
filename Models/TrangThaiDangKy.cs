using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class TrangThaiDangKy
{
    public int MaTrangThai { get; set; }

    public string TenTrangThai { get; set; } = null!;

    public virtual ICollection<DangKyPhong> DangKyPhongs { get; set; } = new List<DangKyPhong>();
}
