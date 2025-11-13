using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HUIT_Library.Controllers
{
    /// <summary>
    /// API Controller đơn giản cho tìm kiếm phòng trống (dành cho người dùng web)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AvailableRoomController : ControllerBase
    {
        private readonly IAvailableRoomService _availableRoomService;
        private readonly ILogger<AvailableRoomController> _logger;

        public AvailableRoomController(IAvailableRoomService availableRoomService, ILogger<AvailableRoomController> logger)
        {
            _availableRoomService = availableRoomService;
            _logger = logger;
        }

        /// <summary>
        /// 🔍 Tìm kiếm phòng trống theo thời gian và loại phòng
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> FindAvailableRooms([FromBody] FindAvailableRoomRequest request)
        {
            try
            {
                // Validation
                if (request.MaLoaiPhong <= 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn loại phòng hợp lệ." });
                }

                if (request.ThoiGianBatDau == default)
                {
                    return BadRequest(new { message = "Vui lòng chọn thời gian bắt đầu hợp lệ." });
                }

                if (request.ThoiGianSuDung <= 0 || request.ThoiGianSuDung > 8)
                {
                    return BadRequest(new { message = "Thời gian sử dụng phải từ 1 đến 8 giờ." });
                }

                try
                {
                    var results = await _availableRoomService.FindAvailableRoomsAsync(request);

                    if (!results.Any())
                    {
                        return Ok(new
                        {
                            message = "Không có phòng trống trong thời gian này.",
                            data = new List<AvailableRoomDto>()
                        });
                    }

                    return Ok(new
                    {
                        message = $"Tìm thấy {results.Count} phòng trống.",
                        data = results
                    });
                }
                catch (SqlException sqlEx)
                {
                    // Handle stored procedure errors
                    _logger.LogError(sqlEx, "SQL error from stored procedure");

                    var errorMessage = sqlEx.Message switch
                    {
                        var msg when msg.Contains("Vui lòng nhập thời gian bắt đầu hợp lệ") => "Thời gian bắt đầu không hợp lệ.",
                        var msg when msg.Contains("Loại phòng không tồn tại") => "Loại phòng không tồn tại.",
                        _ => "Đã xảy ra lỗi khi tìm kiếm phòng trống."
                    };

                    return BadRequest(new { message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding available rooms");
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn." });
            }
        }
            /// <summary>
            /// 🏠 Chi tiết phòng khi người dùng click vào (bao gồm tài nguyên)
            /// </summary>
            [HttpGet("detail/{maPhong}")]
            public async Task<IActionResult> GetRoomDetail(int maPhong)
            {
                try
                {
                    var result = await _availableRoomService.GetRoomDetailAsync(maPhong);

                    if (result == null)
                    {
                        return NotFound(new { message = "Không tìm thấy thông tin phòng." });
                    }

                    return Ok(new
                    {
                        message = $"Thông tin chi tiết phòng {result.TenPhong}.",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting room detail for room {RoomId}", maPhong);
                    return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin phòng." });
                }
            }

        /// <summary>
        /// 📋 Lấy danh sách loại phòng
        /// </summary>
        [HttpGet("room-types")]
        public async Task<IActionResult> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _availableRoomService.GetRoomTypesAsync();

                return Ok(new
                {
                    message = "Danh sách loại phòng.",
                    data = roomTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room types");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách loại phòng." });
            }
        }
    }
}