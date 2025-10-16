using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LoaiPhong
{
    public int MaLoaiPhong { get; set; }

    public string TenLoaiPhong { get; set; } = null!;

    public string ViTri { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? TrangThietBi { get; set; }

    public string SoLuongChoNgoi { get; set; } = null!;

    public string ThoiGianSuDung { get; set; } = null!;

    public int? ThoiLuongToiDa { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
