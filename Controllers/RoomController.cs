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
                return NotFound(new { message = "Không có phòng nào phù hợp với tiêu chí của bạn." });

            return Ok(results);
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetDetails(int roomId)
        {
            var details = await _roomService.GetRoomDetailsAsync(roomId);
            if (details == null) return NotFound();
            return Ok(details);
        }

        /// <summary>
        /// Lấy thông tin giới hạn số lượng người cho tất cả loại phòng
        /// </summary>
        [HttpGet("capacity-limits")]
        public async Task<IActionResult> GetAllCapacityLimits()
        {
            var allLimits = await _roomService.GetAllRoomCapacityLimitsAsync();
            if (!allLimits.Any())
                return NotFound(new { message = "Không tìm thấy thông tin loại phòng nào." });

            return Ok(allLimits);
        }

        /// <summary>
        /// Lấy thông tin trạng thái phòng hiện tại (số phòng trống/bận)
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetCurrentRoomStatus()
        {
            try
            {
                var status = await _roomService.GetCurrentRoomStatusAsync();
                return Ok(new
                {
                    success = true,
                    data = status,
                    message = $"Hiện tại có {status.SoPhongTrong}/{status.TongSoPhong} phòng trống ({status.PhanTramPhongTrong}%)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi khi lấy thông tin trạng thái phòng",
                    error = ex.Message
                });
            }
        }
    }
}
