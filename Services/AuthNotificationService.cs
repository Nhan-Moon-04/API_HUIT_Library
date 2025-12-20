using HUIT_Library.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HUIT_Library.Services
{
    /// <summary>
    /// Service ?? g?i realtime notification khi có s? ki?n authentication
    /// (logout, force logout, session revoke, etc.)
    /// </summary>
    public interface IAuthNotificationService
    {
        /// <summary>
        /// Thông báo cho m?t session c? th? r?ng nó ?ã b? ??ng xu?t
        /// </summary>
        Task NotifySessionLogoutAsync(int sessionId, string reason);

        /// <summary>
        /// Thông báo cho t?t c? session c?a user (tr? session hi?n t?i)
        /// </summary>
        Task NotifyUserSessionsLogoutAsync(int userId, int excludeSessionId, string reason);
    }

    public class AuthNotificationService : IAuthNotificationService
    {
        private readonly IHubContext<AuthHub> _authHubContext;
        private readonly ILogger<AuthNotificationService> _logger;

        public AuthNotificationService(
            IHubContext<AuthHub> authHubContext,
            ILogger<AuthNotificationService> logger)
        {
            _authHubContext = authHubContext;
            _logger = logger;
        }

        /// <summary>
        /// G?i message ??ng xu?t cho m?t session c? th?
        /// </summary>
        public async Task NotifySessionLogoutAsync(int sessionId, string reason)
        {
            try
            {
                var message = new
                {
                    Type = "ForceLogout",
                    SessionId = sessionId,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                    Message = "Phiên ??ng nh?p c?a b?n ?ã b? ??ng xu?t t? thi?t b? khác"
                };

                // G?i ??n group Session_<sessionId>
                await _authHubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("ForceLogout", message);

                _logger.LogInformation("?? Sent ForceLogout notification to Session {SessionId}: {Reason}",
                    sessionId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ForceLogout notification to Session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// G?i message ??ng xu?t cho t?t c? session c?a user (tr? session hi?n t?i)
        /// </summary>
        public async Task NotifyUserSessionsLogoutAsync(int userId, int excludeSessionId, string reason)
        {
            try
            {
                var message = new
                {
                    Type = "ForceLogout",
                    UserId = userId,
                    ExcludeSessionId = excludeSessionId,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                    Message = "T?t c? thi?t b? khác ?ã ???c ??ng xu?t"
                };

                // G?i ??n group User_<userId>
                // Client s? t? check n?u sessionId == excludeSessionId thì b? qua
                await _authHubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ForceLogoutOthers", message);

                _logger.LogInformation("?? Sent ForceLogoutOthers notification to User {UserId} (exclude Session {ExcludeSessionId}): {Reason}",
                    userId, excludeSessionId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ForceLogoutOthers notification to User {UserId}", userId);
            }
        }
    }
}
