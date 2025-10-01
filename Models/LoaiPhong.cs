using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LoaiPhong
{
    public int MaLoai { get; set; }

    public string MaCode { get; set; } = null!;

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public int? ThuTu { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
