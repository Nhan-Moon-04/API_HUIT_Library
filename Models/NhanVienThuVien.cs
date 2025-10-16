using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class NhanVienThuVien
{
    public int MaNguoiDung { get; set; }

    public string MaNhanVien { get; set; } = null!;

    public int MaChucVu { get; set; }

    public virtual ICollection<LichTruc> LichTrucs { get; set; } = new List<LichTruc>();

    public virtual ChucVu MaChucVuNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
