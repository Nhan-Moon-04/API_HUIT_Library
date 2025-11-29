using HUIT_Library.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HUIT_Library.DTOs.Request;

namespace HUIT_Library.Controllers
{
    /// <summary>
    /// API Controller cho quản lý vi phạm
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v2/[controller]")]
    public class ViolationController : ControllerBase
    {
        private readonly IViolationService _violationService;
        private readonly ILogger<ViolationController> _logger;

        public ViolationController(
           IViolationService violationService,
           ILogger<ViolationController> logger)
        {
            _violationService = violationService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng.");
            }
            return userId;
        }

        /// <summary>
        /// Lấy danh sách vi phạm của người dùng
        /// </summary>
        [HttpPost("my-violations")]
        public async Task<IActionResult> GetMyViolations([FromBody] GetMyViolationsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _violationService.GetUserViolationsAsync(userId, request.PageNumber, request.PageSize);
                return Ok(new { success = true, data = result, total = result.Count });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in GetMyViolations");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyViolations");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// Kiểm tra vi phạm gần đây
        /// </summary>
        [HttpGet("recent-check")]
        public async Task<IActionResult> CheckRecentViolations([FromQuery] int monthsBack = 6)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _violationService.CheckRecentViolationsAsync(userId, monthsBack);

                return Ok(new
                {
                    success = true,
                    hasViolations = result.HasViolations,
                    violationCount = result.ViolationCount,
                    message = result.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in CheckRecentViolations");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckRecentViolations");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// Lấy chi tiết vi phạm
        /// </summary>
        [HttpGet("details/{maViPham}")]
        public async Task<IActionResult> GetViolationDetail(int maViPham)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _violationService.GetViolationDetailAsync(userId, maViPham);

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông tin vi phạm." });
                }

                return Ok(new { success = true, data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in GetViolationDetail");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetViolationDetail for violation {MaViPham}", maViPham);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// Lấy danh sách vi phạm của một phiếu đăng ký phòng
        /// </summary>
        [HttpGet("booking/{maDangKy}")]
        public async Task<IActionResult> GetBookingViolations(int maDangKy)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _violationService.GetBookingViolationsAsync(userId, maDangKy);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = result.Count > 0
                        ? $"Tìm thấy {result.Count} vi phạm"
                        : "Không có vi phạm nào"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in GetBookingViolations");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBookingViolations for booking {MaDangKy}", maDangKy);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }
    }
}
