using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // Allow anonymous create: if user is authenticated use user id from JWT, otherwise accept MaNguoiDung in body
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Lấy user ID từ token (JWT)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong token." });

            // Gán lại vào request nếu model có property MaNguoiDung
            request.MaNguoiDung = userId;

            // ✅ Gọi service tạo booking
            var (success, message) = await _bookingService.CreateBookingRequestAsync(userId, request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [Authorize]
        [HttpPost("extend")]
        public async Task<IActionResult> Extend([FromBody] ExtendBookingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Người dùng chưa đăng nhập hoặc thông tin không hợp lệ." });
            }

            var (success, message) = await _bookingService.ExtendBookingAsync(userId, request);
            if (!success) return BadRequest(new { message });

            return Ok(new { message });
        }

        [Authorize]
        [HttpPost("complete/{maDangKy}")]
        public async Task<IActionResult> Complete(int maDangKy)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Người dùng chưa đăng nhập hoặc thông tin không hợp lệ." });

            var (success, message) = await _bookingService.CompleteBookingAsync(userId, maDangKy);
            if (!success) return BadRequest(new { message });

            return Ok(new { message });
        }



        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Người dùng chưa đăng nhập hoặc thông tin không hợp lệ." });

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            try
            {
                var history = await _bookingService.GetBookingHistoryAsync(userId, pageNumber, pageSize);

                return Ok(new
                {
                    success = true,
                    data = history,
                    pagination = new
                    {
                        pageNumber,
                        pageSize,
                        totalItems = history.Count
                    },
                    message = "Lấy lịch sử mượn phòng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy lịch sử mượn phòng" });
            }
        }

        [Authorize]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Người dùng chưa đăng nhập hoặc thông tin không hợp lệ." });

            try
            {
                var currentBookings = await _bookingService.GetCurrentBookingsAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = currentBookings,
                    message = $"Lấy danh sách đăng ký hiện tại thành công. Tìm thấy {currentBookings.Count} đăng ký."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy danh sách đăng ký hiện tại" });
            }
        }
    }
}
