using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LichHoatDongThuVien
{
    public int MaLichHoatDong { get; set; }

    public string? KhuVuc { get; set; }

    public byte ThuTrongTuan { get; set; }

    public TimeOnly? GioMoCua { get; set; }

    public TimeOnly? GioDongCua { get; set; }

    public bool HoatDong { get; set; }

    public string? GhiChu { get; set; }
}
