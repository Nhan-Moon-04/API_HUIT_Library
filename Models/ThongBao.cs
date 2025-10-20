using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class ThongBao
{
    public int MaThongBao { get; set; }

    public int MaNguoiDung { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? LoaiThongBao { get; set; }

    public bool? DaDoc { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
