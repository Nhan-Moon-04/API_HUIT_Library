namespace HUIT_Library.DTOs.DTO
{
  /// <summary>
    /// DTO ??n gi?n cho danh sách phòng tr?ng (ch? thông tin c?n thi?t cho user)
    /// </summary>
    public class AvailableRoomDto
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
   public string SucChua { get; set; } = string.Empty;
   public string? ViTri { get; set; }
    }

    /// <summary>
    /// DTO chi ti?t phòng khi user click vào (bao g?m tài nguyên)
    /// </summary>
    public class RoomDetailDto
  {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public string SucChua { get; set; } = string.Empty;
    public string? ViTri { get; set; }
        public string? MoTa { get; set; }
        
        /// <summary>
     /// Danh sách tài nguyên/thi?t b? c?a phòng
      /// </summary>
        public List<RoomResourceDto> TaiNguyen { get; set; } = new List<RoomResourceDto>();
    }

    /// <summary>
    /// DTO cho tài nguyên phòng
    /// </summary>
    public class RoomResourceDto
    {
        public string TenTaiNguyen { get; set; } = string.Empty;
        public int? SoLuong { get; set; }
    public string? TinhTrang { get; set; }
    }
}