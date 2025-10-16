using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LichTrangThaiPhong
{
    public int MaLich { get; set; }

    public int MaPhong { get; set; }

    public DateOnly Ngay { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}
