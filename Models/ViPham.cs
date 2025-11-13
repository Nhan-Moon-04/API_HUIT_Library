using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class ViPham
{
    public int MaViPham { get; set; }

    public int MaSuDung { get; set; }

    public int? MaQuyDinh { get; set; }

    public DateTime? NgayLap { get; set; }

    public string? TrangThaiXuLy { get; set; }

    public string? GhiChu { get; set; }

    public byte[]? BienBan { get; set; }

    public virtual QuyDinhViPham? MaQuyDinhNavigation { get; set; }

    public virtual SuDungPhong MaSuDungNavigation { get; set; } = null!;
}
