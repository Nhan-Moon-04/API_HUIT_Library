using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class ThongBao
{
    public long MaThongBao { get; set; }

    public int MaNguoiDung { get; set; }

    public int? MaDat { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? Loai { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual DatPhong? MaDatNavigation { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
