using HUIT_Library.DTOs.DTO;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [Authorize]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var items = await _notificationService.GetNotificationsForUserAsync(userId);
            if (!items.Any())
                return Ok(new { message = "Hi?n t?i b?n ch?a có thông báo nào." });

            return Ok(items);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var details = await _notificationService.GetNotificationDetailsAsync(userId, id);
            if (details == null) return NotFound();

            return Ok(details);
        }
    }
}
