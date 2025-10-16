using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class QuanLyKyThuat
{
    public int MaNguoiDung { get; set; }

    public string? MaQuanTri { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
