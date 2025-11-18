using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class Phong
{
    public int MaPhong { get; set; }

    public string TenPhong { get; set; } = null!;

    public int MaLoaiPhong { get; set; }

    public string? TinhTrang { get; set; }

    public virtual ICollection<DangKyPhong> DangKyPhongs { get; set; } = new List<DangKyPhong>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<LichDangKy> LichDangKies { get; set; } = new List<LichDangKy>();

    public virtual ICollection<LichTrangThaiPhong> LichTrangThaiPhongs { get; set; } = new List<LichTrangThaiPhong>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual ICollection<PhongTaiNguyen> PhongTaiNguyens { get; set; } = new List<PhongTaiNguyen>();
}
