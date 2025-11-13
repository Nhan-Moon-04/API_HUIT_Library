using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotification _notificationService;
        private readonly ILogger<NotificationController> _logger;
        private readonly INotificationService _notificationServiceOld;

        public NotificationController(INotification notificationService, INotificationService notificationService1, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _notificationServiceOld = notificationService1;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách thông báo của người dùng
        /// </summary>
        [Authorize]
        [HttpGet("list")]
        public async Task<IActionResult> GetNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                var notifications = await _notificationService.GetNotificationsAsync(userId, pageNumber, pageSize);
                var totalUnread = await _notificationService.CountNotificationAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = notifications,
                    pagination = new
                    {
                        pageNumber,
                        pageSize,
                        totalItems = notifications.Count
                    },
                    totalUnread,
                    message = notifications.Any()
                        ? "Lấy danh sách thông báo thành công."
                        : "Hiện tại bạn chưa có thông báo nào."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông báo." });
            }
        }

        /// <summary>
        /// Đếm số thông báo chưa đọc
        /// </summary>
        [Authorize]
        [HttpGet("count")]
        public async Task<IActionResult> CountUnreadNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                var count = await _notificationService.CountNotificationAsync(userId);

                return Ok(new
                {
                    success = true,
                    count,
                    message = $"Bạn có {count} thông báo chưa đọc."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting notifications");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đếm thông báo." });
            }
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        [Authorize]
        [HttpPost("{notificationId}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                var success = await _notificationService.MarkAsReadAsync(notificationId, userId);

                if (!success)
                    return NotFound(new { success = false, message = "Không tìm thấy thông báo." });

                return Ok(new { success = true, message = "Đánh dấu đã đọc thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật thông báo." });
            }
        }

        /// <summary>
        /// Lấy chi tiết một thông báo
        /// </summary>
        [Authorize]
        [HttpPost("NotificationDetail")]
        public async Task<IActionResult> GetNotificationDetail([FromBody] int notificationId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                var notificationDetail = await _notificationServiceOld.GetNotificationDetailsAsync(notificationId);
                if (notificationDetail == null)
                    return NotFound(new { success = false, message = "Không tìm thấy thông báo." });

                return Ok(new
                {
                    success = true,
                    data = notificationDetail,
                    message = "Lấy chi tiết thông báo thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification detail");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy chi tiết thông báo." });
            }
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo là đã đọc
        /// </summary>
        [Authorize]
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng." });

                var success = await _notificationService.MarkAllAsReadAsync(userId);

                if (!success)
                    return BadRequest(new { success = false, message = "Không thể cập nhật thông báo." });

                return Ok(new { success = true, message = "Đánh dấu tất cả thông báo đã đọc thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật thông báo." });
            }
        }
    }
}
