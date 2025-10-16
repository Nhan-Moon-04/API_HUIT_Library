using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class LichTruc
{
    public int MaLichTruc { get; set; }

    public string MaNhanVien { get; set; } = null!;

    public DateOnly NgayTruc { get; set; }

    public string CaTruc { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual NhanVienThuVien MaNhanVienNavigation { get; set; } = null!;
}
