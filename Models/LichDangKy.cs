using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LichDangKy
{
    public int MaLich { get; set; }

    public int MaPhong { get; set; }

    public int? MaNguoiDung { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public string? GhiChu { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}
