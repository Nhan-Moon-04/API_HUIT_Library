using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class PhongTaiNguyen
{
    public int MaPhong { get; set; }

    public int MaTaiNguyen { get; set; }

    public int? SoLuong { get; set; }

    public string? TinhTrang { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual TaiNguyen MaTaiNguyenNavigation { get; set; } = null!;
}
