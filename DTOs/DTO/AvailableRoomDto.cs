namespace HUIT_Library.DTOs.DTO
{
 /// <summary>
    /// DTO cho danh sách phòng tr?ng
    /// </summary>
 public class AvailableRoomDto
    {
 public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
 public string? SucChua { get; set; }
      public string? ViTri { get; set; }
        public string? MoTa { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public bool IsAvailable { get; set; } = true;
        
        /// <summary>
    /// Danh sách thi?t b? chính c?a phòng
     /// </summary>
      public List<string> ThietBiChinh { get; set; } = new List<string>();
    }
}