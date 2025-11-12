using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    /// <summary>
    /// API Controller cho qu?n lý ??t phòng (phiên b?n module hóa)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v2/[controller]")]
    public class BookingManagementController : ControllerBase
    {
        private readonly IBookingManagementService _bookingManagementService;
        private readonly ILogger<BookingManagementController> _logger;

        public BookingManagementController(
            IBookingManagementService bookingManagementService,
            ILogger<BookingManagementController> logger)
        {
            _bookingManagementService = bookingManagementService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Không th? xác ??nh ng??i dùng.");
            }
            return userId;
        }

        /// <summary>
        /// T?o yêu c?u ??t phòng m?i
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateBookingRequest([FromBody] CreateBookingRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _bookingManagementService.CreateBookingRequestAsync(userId, request);

                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in CreateBookingRequest");
                return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateBookingRequest");
                return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
        }

        /// <summary>
        /// Gia h?n th?i gian s? d?ng phòng
        /// </summary>
        [HttpPost("extend")]
        public async Task<IActionResult> ExtendBooking([FromBody] ExtendBookingRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _bookingManagementService.ExtendBookingAsync(userId, request);

                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in ExtendBooking");
                return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtendBooking for booking {MaDangKy}", request.MaDangKy);
                return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
        }

        /// <summary>
        /// Xác nh?n tr? phòng (hoàn thành s? d?ng)
        /// </summary>
        [HttpPost("complete/{maDangKy}")]
        public async Task<IActionResult> CompleteBooking(int maDangKy)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _bookingManagementService.CompleteBookingAsync(userId, maDangKy);

                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in CompleteBooking");
                return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteBooking for booking {MaDangKy}", maDangKy);
                return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
        }

        /// <summary>
        /// H?y ??t phòng v?i lý do
        /// </summary>
        [HttpPost("cancel/{maDangKy}")]
        public async Task<IActionResult> CancelBooking(int maDangKy, [FromBody] CancelReasonRequest request)
        {
            try
            {
                // Ki?m tra lý do h?y
                if (string.IsNullOrWhiteSpace(request?.LyDoHuy))
                {
                    return BadRequest(new { success = false, message = "Vui lòng nh?p lý do h?y ??ng ký." });
                }

                var userId = GetCurrentUserId();

                // T?o CancelBookingRequest object
                var cancelRequest = new CancelBookingRequest
                {
                    MaDangKy = maDangKy,
                    LyDoHuy = request.LyDoHuy,
                    GhiChu = request.GhiChu
                };

                var result = await _bookingManagementService.CancelBookingAsync(userId, cancelRequest);

                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in CancelBooking");
                return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBooking for booking {MaDangKy}", maDangKy);
                return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
        }
    }

    /// <summary>
    /// Request ??n gi?n ch? ch?a lý do h?y
    /// </summary>
    public class CancelReasonRequest
    {
        /// <summary>
        /// Lý do h?y ??ng ký (b?t bu?c)
        /// </summary>
        public string LyDoHuy { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú thêm (tùy ch?n)
        /// </summary>
        public string? GhiChu { get; set; }
    }
}