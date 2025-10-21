using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class PhienChat
{
    public int MaPhienChat { get; set; }

    public int MaNguoiDung { get; set; }

    public int? MaNhanVien { get; set; }

    public bool? CoBot { get; set; }

    public DateTime? ThoiGianBatDau { get; set; }

    public DateTime? ThoiGianKetThuc { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;

    public virtual ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
}
