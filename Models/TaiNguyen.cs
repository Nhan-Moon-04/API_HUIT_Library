using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class TaiNguyen
{
    public int MaTaiNguyen { get; set; }

    public string TenTaiNguyen { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<PhongTaiNguyen> PhongTaiNguyens { get; set; } = new List<PhongTaiNguyen>();
}
