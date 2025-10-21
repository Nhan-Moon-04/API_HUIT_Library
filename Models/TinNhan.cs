using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class TinNhan
{
    public int MaTinNhan { get; set; }

    public int MaPhienChat { get; set; }

    public int MaNguoiGui { get; set; }

    public string NoiDung { get; set; } = null!;

    public DateTime? ThoiGianGui { get; set; }

    public bool? LaBot { get; set; }

    public virtual PhienChat MaPhienChatNavigation { get; set; } = null!;
}
