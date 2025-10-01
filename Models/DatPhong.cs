using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class DatPhong
{
    public int MaDat { get; set; }

    public string MaCode { get; set; } = null!;

    public int MaNguoiDung { get; set; }

    public int NguoiTao { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public string? MucDich { get; set; }

    public int? SoNguoiThamGia { get; set; }

    public string? YeuCauDacBiet { get; set; }

    public string MaTrangThai { get; set; } = null!;

    public int? NguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public string? GhiChuDuyet { get; set; }

    public string? LyDoHuy { get; set; }

    public int? NguoiHuy { get; set; }

    public DateTime? NgayHuy { get; set; }

    public byte? DanhGia { get; set; }

    public string? PhanHoi { get; set; }

    public int MaPhong { get; set; }

    public string? TinhTrangSau { get; set; }

    public string? MoTaHuHong { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual TrangThaiDat MaTrangThaiNavigation { get; set; } = null!;

    public virtual NguoiDung? NguoiDuyetNavigation { get; set; }

    public virtual NguoiDung? NguoiHuyNavigation { get; set; }

    public virtual NguoiDung NguoiTaoNavigation { get; set; } = null!;

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
}
