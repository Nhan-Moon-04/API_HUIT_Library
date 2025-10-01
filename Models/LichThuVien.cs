using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LichThuVien
{
    public int MaLich { get; set; }

    public byte ThuTrongTuan { get; set; }

    public TimeOnly GioMo { get; set; }

    public TimeOnly GioDong { get; set; }

    public bool? CoMoCua { get; set; }

    public string? MoTa { get; set; }
}
