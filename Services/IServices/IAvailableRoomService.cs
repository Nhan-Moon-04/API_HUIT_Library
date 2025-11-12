using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;

namespace HUIT_Library.Services.IServices
{
    /// <summary>
  /// Interface cho d?ch v? tìm ki?m phòng tr?ng
  /// </summary>
  public interface IAvailableRoomService
    {
        /// <summary>
        /// Tìm ki?m phòng tr?ng theo th?i gian và lo?i phòng
   /// </summary>
        /// <param name="request">Thông tin tìm ki?m</param>
        /// <returns>Danh sách phòng tr?ng</returns>
        Task<List<AvailableRoomDto>> FindAvailableRoomsAsync(FindAvailableRoomRequest request);

     /// <summary>
     /// Ki?m tra phòng c? th? có tr?ng không
        /// </summary>
        /// <param name="maPhong">Mã phòng</param>
        /// <param name="thoiGianBatDau">Th?i gian b?t ??u</param>
        /// <param name="thoiGianKetThuc">Th?i gian k?t thúc</param>
      /// <returns>True n?u phòng tr?ng</returns>
   Task<bool> IsRoomAvailableAsync(int maPhong, DateTime thoiGianBatDau, DateTime thoiGianKetThuc);
   
        /// <summary>
        /// L?y danh sách lo?i phòng
        /// </summary>
    /// <returns>Danh sách lo?i phòng</returns>
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
        public string? MoTa { get; set; }
        public string? SoLuongChoNgoi { get; set; }
        public int SoPhongKhaDung { get; set; }
    }
}