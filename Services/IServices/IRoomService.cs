using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.Services.IServices
{
    public interface IRoomService
    {
        Task<IEnumerable<RoomDto>> SearchRoomsAsync(SearchRoomRequest request);
        Task<RoomDetailsDto?> GetRoomDetailsAsync(int roomId);
    }
}
