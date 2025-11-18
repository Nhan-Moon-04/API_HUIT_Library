namespace HUIT_Library.DTOs.Response
{
    /// <summary>
    /// DTO cho thông tin ?ánh giá
    /// </summary>
    public class RatingDto
    {
public int MaDanhGia { get; set; }
 public int MaNguoiDung { get; set; }
      public string? TenNguoiDung { get; set; }
        public string LoaiDoiTuong { get; set; } = string.Empty;
        public int MaDoiTuong { get; set; }
     public string? TenDoiTuong { get; set; }
   public int? DiemDanhGia { get; set; }
        public string? NoiDung { get; set; }
  public DateTime? NgayDanhGia { get; set; }
        public bool CoTheChinhSua { get; set; } // User có th? ch?nh s?a ?ánh giá này không
    }

    /// <summary>
    /// DTO cho th?ng kê ?ánh giá
    /// </summary>
    public class RatingStatisticsDto
    {
        public string LoaiDoiTuong { get; set; } = string.Empty;
     public int MaDoiTuong { get; set; }
        public string? TenDoiTuong { get; set; }
        public int TongSoDanhGia { get; set; }
        public double DiemTrungBinh { get; set; }
        public int Sao1 { get; set; }
     public int Sao2 { get; set; }
        public int Sao3 { get; set; }
   public int Sao4 { get; set; }
        public int Sao5 { get; set; }
    }

 /// <summary>
    /// DTO cho ?ánh giá chi ti?t v?i thông tin m? r?ng
    /// </summary>
    public class RatingDetailDto : RatingDto
    {
        public string? MaCode { get; set; } // Mã code c?a user n?u có
        public int? MaDangKy { get; set; } // Mã ??ng ký liên quan (n?u có)
     public DateTime? NgaySuDung { get; set; } // Ngày s? d?ng d?ch v?
        public bool DaXacThuc { get; set; } // ?ã xác th?c ?ánh giá hay ch?a
    }

    /// <summary>
  /// DTO cho trang ?ánh giá có phân trang
    /// </summary>
    public class PagedRatingResponse
    {
        public List<RatingDto> Data { get; set; } = new List<RatingDto>();
  public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
  public int TotalPages { get; set; }
      public RatingStatisticsDto? ThongKe { get; set; }
    }
}