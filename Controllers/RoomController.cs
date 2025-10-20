using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRoomRequest request)
        {
            var results = await _roomService.SearchRoomsAsync(request);
            if (!results.Any())
                return NotFound(new { message = "Không có phòng nào phù h?p v?i tiêu chí c?a b?n." });

            return Ok(results);
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetDetails(int roomId)
        {
            var details = await _roomService.GetRoomDetailsAsync(roomId);
            if (details == null) return NotFound();
            return Ok(details);
        }
    }
}
