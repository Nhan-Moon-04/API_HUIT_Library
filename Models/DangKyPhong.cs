using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class DangKyPhong
{
    public int MaDangKy { get; set; }

    public int MaNguoiDung { get; set; }

    public int? MaPhong { get; set; }

    public int MaLoaiPhong { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public string? LyDo { get; set; }

    public int? MaTrangThai { get; set; }

    public DateOnly? NgayMuon { get; set; }

    public int? NguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public int? SoLuong { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;

    public virtual Phong? MaPhongNavigation { get; set; }

    public virtual TrangThaiDangKy? MaTrangThaiNavigation { get; set; }

    public virtual SuDungPhong? SuDungPhong { get; set; }
}
