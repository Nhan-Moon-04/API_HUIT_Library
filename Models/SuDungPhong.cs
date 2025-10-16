using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class SuDungPhong
{
    public int MaSuDung { get; set; }

    public int MaDangKy { get; set; }

    public DateTime? GioBatDauThucTe { get; set; }

    public DateTime? GioKetThucThucTe { get; set; }

    public string? TinhTrangPhong { get; set; }

    public string? GhiChu { get; set; }

    public virtual DangKyPhong MaDangKyNavigation { get; set; } = null!;

    public virtual ICollection<ViPham> ViPhams { get; set; } = new List<ViPham>();
}
