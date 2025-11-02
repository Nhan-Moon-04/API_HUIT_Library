using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LoaiPhong
{
    public int MaLoaiPhong { get; set; }

    public string TenLoaiPhong { get; set; } = null!;

    public int ViTri { get; set; }

    public string? MoTa { get; set; }

    public string? TrangThietBi { get; set; }

    public int SoLuongChoNgoi { get; set; }

    public string ThoiGianSuDung { get; set; } = null!;

    public int? ThoiLuongToiDa { get; set; }

    public virtual ICollection<DangKyPhong> DangKyPhongs { get; set; } = new List<DangKyPhong>();

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
