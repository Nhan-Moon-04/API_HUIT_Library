namespace HUIT_Library.DTOs.Response
{
  public class CurrentBookingDto
    {
        public int MaDangKy { get; set; }
   public int? MaPhong { get; set; }
        public string? TenPhong { get; set; }
     public string? TenLoaiPhong { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? LyDo { get; set; }
public int? SoLuong { get; set; }
        public string? GhiChu { get; set; }
        public int MaTrangThai { get; set; }
        public string? TenTrangThai { get; set; }
        public DateTime? NgayDuyet { get; set; }
    public DateOnly? NgayMuon { get; set; }
    
        // Thông tin thêm
        public bool CanStart { get; set; } // Có th? b?t ??u s? d?ng không
        public bool CanExtend { get; set; } // Có th? gia h?n không
        public bool CanComplete { get; set; } // Có th? tr? phòng không
        public string StatusDescription { get; set; } = string.Empty; // Mô t? tr?ng thái chi ti?t
     public int MinutesUntilStart { get; set; } // S? phút ??n khi b?t ??u
     public int MinutesRemaining { get; set; } // S? phút còn l?i

        // ? Thêm thông tin vi ph?m
        public bool CoBienBan { get; set; }
        public int SoLuongBienBan { get; set; }

        // Thông tin ?ánh giá (cho booking ?ã completed)
        public bool DaDanhGia { get; set; }
        public bool CoTheDanhGia { get; set; }
        public string? TrangThaiDanhGia { get; set; }
    }
}