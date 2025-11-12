using HUIT_Library.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    /// <summary>
    /// API Controller cho xem l?ch s? và tr?ng thái ??t phòng
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v2/[controller]")]
 public class BookingViewController : ControllerBase
 {
        private readonly IBookingViewService _bookingViewService;
        private readonly ILogger<BookingViewController> _logger;

        public BookingViewController(
  IBookingViewService bookingViewService,
            ILogger<BookingViewController> logger)
        {
          _bookingViewService = bookingViewService;
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
      /// L?y l?ch s? ??t phòng
     /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetBookingHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
   {
 try
            {
     var userId = GetCurrentUserId();
    var result = await _bookingViewService.GetBookingHistoryAsync(userId, pageNumber, pageSize);
return Ok(new { success = true, data = result, total = result.Count });
    }
            catch (UnauthorizedAccessException ex)
  {
       _logger.LogWarning(ex, "Unauthorized access in GetBookingHistory");
         return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
     catch (Exception ex)
            {
        _logger.LogError(ex, "Error in GetBookingHistory");
            return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
      }

        /// <summary>
 /// L?y danh sách ??t phòng hi?n t?i
      /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentBookings()
        {
          try
            {
      var userId = GetCurrentUserId();
           var result = await _bookingViewService.GetCurrentBookingsAsync(userId);
      return Ok(new { success = true, data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
      _logger.LogWarning(ex, "Unauthorized access in GetCurrentBookings");
        return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
            catch (Exception ex)
          {
    _logger.LogError(ex, "Error in GetCurrentBookings");
 return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
   }
        }

        /// <summary>
 /// L?y chi ti?t m?t ??t phòng
        /// </summary>
        [HttpGet("details/{maDangKy}")]
      public async Task<IActionResult> GetBookingDetails(int maDangKy)
   {
         try
         {
        var userId = GetCurrentUserId();
        var result = await _bookingViewService.GetBookingDetailsAsync(userId, maDangKy);

    if (result == null)
      {
         return NotFound(new { success = false, message = "Không tìm th?y thông tin ??t phòng." });
                }

        return Ok(new { success = true, data = result });
            }
  catch (UnauthorizedAccessException ex)
          {
      _logger.LogWarning(ex, "Unauthorized access in GetBookingDetails");
                return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
   }
            catch (Exception ex)
            {
           _logger.LogError(ex, "Error in GetBookingDetails for booking {MaDangKy}", maDangKy);
     return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
     }
    }

        /// <summary>
        /// Tìm ki?m l?ch s? ??t phòng
     /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookingHistory(
  [FromQuery] string searchTerm, 
    [FromQuery] int pageNumber = 1, 
      [FromQuery] int pageSize = 10)
 {
      try
     {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
   return BadRequest(new { success = false, message = "Vui lòng nh?p t? khóa tìm ki?m." });
                }

var userId = GetCurrentUserId();
       var result = await _bookingViewService.SearchBookingHistoryAsync(userId, searchTerm, pageNumber, pageSize);
                return Ok(new { success = true, data = result, searchTerm, total = result.Count });
            }
        catch (UnauthorizedAccessException ex)
   {
                _logger.LogWarning(ex, "Unauthorized access in SearchBookingHistory");
           return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
       catch (Exception ex)
      {
         _logger.LogError(ex, "Error in SearchBookingHistory with term '{SearchTerm}'", searchTerm);
                return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
   }
  }
    }
}