using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class TrangThaiDat
{
    public string MaCode { get; set; } = null!;

    public string TenTrangThai { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? Mau { get; set; }

    public bool? TrangThai { get; set; }

    public int? ThuTu { get; set; }

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();
}
