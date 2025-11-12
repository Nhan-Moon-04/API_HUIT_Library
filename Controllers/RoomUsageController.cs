using HUIT_Library.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    /// <summary>
 /// API Controller cho qu?n lý s? d?ng phòng
/// </summary>
    [Authorize]
  [ApiController]
    [Route("api/v2/[controller]")]
 public class RoomUsageController : ControllerBase
  {
        private readonly IRoomUsageService _roomUsageService;
    private readonly ILogger<RoomUsageController> _logger;

        public RoomUsageController(
         IRoomUsageService roomUsageService,
      ILogger<RoomUsageController> logger)
     {
     _roomUsageService = roomUsageService;
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
        /// B?t ??u s? d?ng phòng (check-in)
      /// </summary>
        [HttpPost("start/{maDangKy}")]
   public async Task<IActionResult> StartRoomUsage(int maDangKy)
      {
     try
      {
     var userId = GetCurrentUserId();
    var result = await _roomUsageService.StartRoomUsageAsync(userId, maDangKy);

            if (result.Success)
   {
       return Ok(new { success = true, message = result.Message });
   }

      return BadRequest(new { success = false, message = result.Message });
         }
         catch (UnauthorizedAccessException ex)
            {
    _logger.LogWarning(ex, "Unauthorized access in StartRoomUsage");
     return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
   }
   catch (Exception ex)
 {
 _logger.LogError(ex, "Error in StartRoomUsage for booking {MaDangKy}", maDangKy);
     return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
 }
        }

        /// <summary>
        /// L?y tr?ng thái s? d?ng phòng hi?n t?i
  /// </summary>
        [HttpGet("status/{maDangKy}")]
   public async Task<IActionResult> GetRoomUsageStatus(int maDangKy)
   {
 try
        {
   var userId = GetCurrentUserId();
                var result = await _roomUsageService.GetRoomUsageStatusAsync(userId, maDangKy);

if (result == null)
   {
      return NotFound(new { success = false, message = "Không tìm th?y thông tin s? d?ng phòng." });
            }

   return Ok(new { success = true, data = result });
            }
       catch (UnauthorizedAccessException ex)
            {
    _logger.LogWarning(ex, "Unauthorized access in GetRoomUsageStatus");
return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
            }
         catch (Exception ex)
     {
  _logger.LogError(ex, "Error in GetRoomUsageStatus for booking {MaDangKy}", maDangKy);
      return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
            }
 }

        /// <summary>
   /// C?p nh?t tình tr?ng phòng khi tr?
        /// </summary>
        [HttpPut("update-condition/{maDangKy}")]
        public async Task<IActionResult> UpdateRoomCondition(
   int maDangKy, 
            [FromBody] UpdateRoomConditionRequest request)
       {
    try
   {
      var userId = GetCurrentUserId();
   var result = await _roomUsageService.UpdateRoomConditionAsync(
    userId, maDangKy, request.TinhTrangPhong, request.GhiChu);

     if (result.Success)
 {
        return Ok(new { success = true, message = result.Message });
 }

       return BadRequest(new { success = false, message = result.Message });
         }
     catch (UnauthorizedAccessException ex)
      {
_logger.LogWarning(ex, "Unauthorized access in UpdateRoomCondition");
            return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
       }
        catch (Exception ex)
  {
        _logger.LogError(ex, "Error in UpdateRoomCondition for booking {MaDangKy}", maDangKy);
     return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
      }
        }

/// <summary>
        /// L?y l?ch s? s? d?ng phòng
   /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetRoomUsageHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
  try
     {
    var userId = GetCurrentUserId();
      var result = await _roomUsageService.GetRoomUsageHistoryAsync(userId, pageNumber, pageSize);
  return Ok(new { success = true, data = result, total = result.Count });
      }
   catch (UnauthorizedAccessException ex)
            {
      _logger.LogWarning(ex, "Unauthorized access in GetRoomUsageHistory");
    return Unauthorized(new { success = false, message = "Không có quy?n truy c?p." });
      }
        catch (Exception ex)
  {
   _logger.LogError(ex, "Error in GetRoomUsageHistory");
       return StatusCode(500, new { success = false, message = "?ã x?y ra l?i không mong mu?n." });
   }
  }
    }

    /// <summary>
  /// Request DTO cho c?p nh?t tình tr?ng phòng
 /// </summary>
    public class UpdateRoomConditionRequest
 {
       /// <summary>
        /// Tình tr?ng phòng: "T?t", "Có v?n ??", "H? h?ng", v.v.
    /// </summary>
    public string TinhTrangPhong { get; set; } = string.Empty;

        /// <summary>
   /// Ghi chú thêm v? tình tr?ng phòng
        /// </summary>
        public string? GhiChu { get; set; }
    }
}