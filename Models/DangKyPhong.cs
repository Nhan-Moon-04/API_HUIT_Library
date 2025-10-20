using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class DangKyPhong
{
    public int MaDangKy { get; set; }

    public int MaNguoiDung { get; set; }

    public int MaPhong { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public string? LyDo { get; set; }

    public int? MaTrangThai { get; set; }

    public DateTime? NgayDangKy { get; set; }

    public int? NguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual ICollection<SuDungPhong> SuDungPhongs { get; set; } = new List<SuDungPhong>();
}
