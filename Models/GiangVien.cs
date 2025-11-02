using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class GiangVien
{
    public int MaNguoiDung { get; set; }

    public string MaGiangVien { get; set; } = null!;

    public string? BoMon { get; set; }

    public string? Khoa { get; set; }

    public string? HocHam { get; set; }

    public string? HocVi { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
