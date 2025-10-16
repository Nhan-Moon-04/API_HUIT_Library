using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class QuyDinhViPham
{
    public int MaQuyDinh { get; set; }

    public string TenViPham { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? HinhThucXuLy { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ViPham> ViPhams { get; set; } = new List<ViPham>();
}
