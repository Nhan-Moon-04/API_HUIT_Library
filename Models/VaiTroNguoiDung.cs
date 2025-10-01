using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class VaiTroNguoiDung
{
    public int MaVaiTro { get; set; }

    public string MaCode { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
