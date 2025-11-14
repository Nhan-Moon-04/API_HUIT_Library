using HUIT_Library.DTOs.DTO;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly ILibraryStatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(ILibraryStatisticsService statisticsService, ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        // 📊 Lấy thống kê tổng quan
        [HttpGet("overview")]
        public async Task<IActionResult> GetLibraryStatistics()
        {
            try
            {
                var statistics = await _statisticsService.GetLibraryStatisticsAsync();

                return Ok(new
                {
                    success = true,
                    data = statistics,
                    message = "Lấy thống kê thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting library statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thống kê."
                });
            }
        }

        // 📈 Ghi nhận lượt truy cập
        [HttpPost("visit")]
        public async Task<IActionResult> RecordVisit()
        {
            try
            {
                int? userId = null;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(claim, out var id))
                        userId = id;
                }

                var ip = GetClientIpAddress();

                await _statisticsService.RecordVisitAsync(userId, ip);

                return Ok(new
                {
                    success = true,
                    message = "Ghi nhận truy cập thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording visit");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi ghi nhận truy cập."
                });
            }
        }

        // 👤 Cập nhật trạng thái online
        [HttpPost("online-status")]
        public async Task<IActionResult> UpdateOnlineStatus([FromQuery] bool isOnline = true)
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Unauthorized(new { message = "Cần đăng nhập để cập nhật trạng thái." });

                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(claim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                await _statisticsService.UpdateUserOnlineStatusAsync(userId, isOnline);

                return Ok(new
                {
                    success = true,
                    message = $"Cập nhật trạng thái {(isOnline ? "online" : "offline")} thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating online status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi cập nhật trạng thái."
                });
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress;
                if (ip != null)
                {
                    if (ip.IsIPv4MappedToIPv6)
                        return ip.MapToIPv4().ToString();
                    return ip.ToString();
                }

                return HttpContext.Request.Headers["X-Forwarded-For"]
                    .FirstOrDefault()?
                    .Split(',').First().Trim()
                    ?? HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                    ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
