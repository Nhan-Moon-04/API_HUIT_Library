using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;

namespace HUIT_Library.Services.IServices
{
    /// <summary>
  /// Interface cho d?ch v? tìm ki?m phòng tr?ng (simplified cho user web)
  /// </summary>
  public interface IAvailableRoomService
    {
     /// <summary>
        /// Tìm ki?m phòng tr?ng theo th?i gian và lo?i phòng
    /// </summary>
  Task<List<AvailableRoomDto>> FindAvailableRoomsAsync(FindAvailableRoomRequest request);

        /// <summary>
        /// L?y chi ti?t phòng khi user click vào (bao g?m tài nguyên)
      /// </summary>
    Task<RoomDetailDto?> GetRoomDetailAsync(int maPhong);
   
     /// <summary>
      /// L?y danh sách lo?i phòng ??n gi?n
/// </summary>
        Task<List<RoomTypeSimpleDto>> GetRoomTypesAsync();
    }
}

namespace HUIT_Library.DTOs.DTO
{
    /// <summary>
    /// DTO ??n gi?n cho lo?i phòng
  /// </summary>
    public class RoomTypeSimpleDto
    {
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; } = string.Empty;
  public string SoLuongChoNgoi { get; set; } = string.Empty;
        public int SoPhongKhaDung { get; set; }
    }
}