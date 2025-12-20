using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace HUIT_Library.Hubs
{
    /// <summary>
    /// SignalR Hub ?? x? lý authentication events realtime
    /// (Logout thi?t b?, force logout, session revoke, etc.)
    /// </summary>
    [Authorize]
    public class AuthHub : Hub
    {
        private readonly ILogger<AuthHub> _logger;
        
        // Mapping: SessionId -> ConnectionId (?? track connection c?a t?ng session)
        private static readonly ConcurrentDictionary<int, HashSet<string>> SessionConnections = new();
        
        // Mapping: UserId -> List<ConnectionId> (?? track t?t c? connection c?a user)
        private static readonly ConcurrentDictionary<int, HashSet<string>> UserConnections = new();

        public AuthHub(ILogger<AuthHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                // L?y userId và sessionId t? JWT token
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionIdClaim = Context.User?.FindFirst("SessionId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("AuthHub connection rejected: Invalid userId");
                    Context.Abort();
                    return;
                }

                if (string.IsNullOrEmpty(sessionIdClaim) || !int.TryParse(sessionIdClaim, out var sessionId))
                {
                    _logger.LogWarning("AuthHub connection rejected: Invalid sessionId for user {UserId}", userId);
                    Context.Abort();
                    return;
                }

                var connectionId = Context.ConnectionId;

                // Add to SessionConnections
                SessionConnections.AddOrUpdate(
                    sessionId,
                    new HashSet<string> { connectionId },
                    (_, existing) => { existing.Add(connectionId); return existing; }
                );

                // Add to UserConnections
                UserConnections.AddOrUpdate(
                    userId,
                    new HashSet<string> { connectionId },
                    (_, existing) => { existing.Add(connectionId); return existing; }
                );

                // Add to user group (?? g?i message cho t?t c? thi?t b? c?a user)
                await Groups.AddToGroupAsync(connectionId, $"User_{userId}");

                // Add to session group (?? g?i message cho thi?t b? c? th?)
                await Groups.AddToGroupAsync(connectionId, $"Session_{sessionId}");

                _logger.LogInformation("? AuthHub connected: User {UserId}, Session {SessionId}, Connection {ConnectionId}",
                    userId, sessionId, connectionId);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthHub.OnConnectedAsync");
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionIdClaim = Context.User?.FindFirst("SessionId")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    var connectionId = Context.ConnectionId;

                    // Remove from UserConnections
                    if (UserConnections.TryGetValue(userId, out var userConns))
                    {
                        userConns.Remove(connectionId);
                        if (userConns.Count == 0)
                            UserConnections.TryRemove(userId, out _);
                    }

                    // Remove from SessionConnections
                    if (!string.IsNullOrEmpty(sessionIdClaim) && int.TryParse(sessionIdClaim, out var sessionId))
                    {
                        if (SessionConnections.TryGetValue(sessionId, out var sessionConns))
                        {
                            sessionConns.Remove(connectionId);
                            if (sessionConns.Count == 0)
                                SessionConnections.TryRemove(sessionId, out _);
                        }
                    }

                    _logger.LogInformation("? AuthHub disconnected: User {UserId}, Session {SessionId}, Connection {ConnectionId}",
                        userId, sessionIdClaim, connectionId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthHub.OnDisconnectedAsync");
            }
        }

        /// <summary>
        /// Ping ?? keep connection alive
        /// </summary>
        public Task Ping()
        {
            return Clients.Caller.SendAsync("Pong");
        }

        /// <summary>
        /// Get thông tin debug v? connections
        /// </summary>
        public Task<object> GetConnectionInfo()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionIdClaim = Context.User?.FindFirst("SessionId")?.Value;

            return Task.FromResult<object>(new
            {
                UserId = userIdClaim,
                SessionId = sessionIdClaim,
                ConnectionId = Context.ConnectionId,
                TotalSessionConnections = SessionConnections.Count,
                TotalUserConnections = UserConnections.Count
            });
        }
    }
}
