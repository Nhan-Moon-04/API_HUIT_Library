using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class Phong
{
    public int MaPhong { get; set; }

    public string MaCode { get; set; } = null!;

    public string TenPhong { get; set; } = null!;

    public int MaLoai { get; set; }

    public string? ViTri { get; set; }

    public byte? Tang { get; set; }

    public int? SucChua { get; set; }

    public string? MoTa { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    public virtual LoaiPhong MaLoaiNavigation { get; set; } = null!;
}
