using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.Services.IServices
{
    public interface IRoomService
    {
        Task<IEnumerable<RoomDto>> SearchRoomsAsync(SearchRoomRequest request);
        Task<RoomDetailsDto?> GetRoomDetailsAsync(int roomId);
        
        /// <summary>
        /// L?y s? l??ng min/max theo lo?i phòng
        /// </summary>
        Task<RoomCapacityLimitsDto?> GetRoomCapacityLimitsAsync(int maLoaiPhong);
        
        /// <summary>
        /// L?y thông tin gi?i h?n s? l??ng ng??i cho t?t c? lo?i phòng
        /// </summary>
        Task<IEnumerable<RoomCapacityLimitsDto>> GetAllRoomCapacityLimitsAsync();
        
        /// <summary>
        /// L?y thông tin tr?ng thái phòng hi?n t?i (tr?ng/b?n)
        /// </summary>
        Task<RoomStatusDto> GetCurrentRoomStatusAsync();
    }
}
