using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HUIT_Library.Middleware
{
    /// <summary>
    /// Middleware ?? validate JWT token v?i session trong database
    /// ??m b?o token b? revoke không th? s? d?ng
    /// </summary>
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, HuitThuVienContext dbContext)
        {
            // Ch? validate khi user ?ã authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // L?y SessionId t? JWT token
                    var sessionIdClaim = context.User.FindFirst("SessionId")?.Value;
                    
                    if (!string.IsNullOrEmpty(sessionIdClaim) && int.TryParse(sessionIdClaim, out var sessionId))
                    {
                        // ? CHECK SESSION TRONG DATABASE
                        var session = await dbContext.UserSessions
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == sessionId);

                        // ? Session không t?n t?i ho?c ?ã b? revoke
                        if (session == null || session.IsRevoked)
                        {
                            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            
                            _logger.LogWarning(
                                "?? Blocked request with REVOKED token. User: {UserId}, Session: {SessionId}, Revoked: {IsRevoked}, Reason: {Reason}",
                                userId, sessionId, session?.IsRevoked, session?.RevokeReason);

                            // Tr? v? 401 Unauthorized
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            
                            await context.Response.WriteAsJsonAsync(new
                            {
                                success = false,
                                message = "Phiên ??ng nh?p không h?p l? ho?c ?ã h?t h?n",
                                reason = session?.RevokeReason ?? "Session not found",
                                code = "SESSION_REVOKED",
                                timestamp = DateTime.UtcNow
                            });

                            return; // ? D?ng request pipeline
                        }

                        // ? CHECK EXPIRATION (n?u có)
                        if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTime.Now)
                        {
                            _logger.LogWarning(
                                "?? Blocked request with EXPIRED token. Session: {SessionId}, Expired at: {ExpiredAt}",
                                sessionId, session.ExpiresAt);

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            
                            await context.Response.WriteAsJsonAsync(new
                            {
                                success = false,
                                message = "Token ?ã h?t h?n",
                                code = "TOKEN_EXPIRED",
                                expiredAt = session.ExpiresAt,
                                timestamp = DateTime.UtcNow
                            });

                            return;
                        }

                        // ? UPDATE LAST ACCESS TIME (optional - ?? tracking)
                        // Note: Ch? update m?i 5 phút ?? tránh overhead
                        var shouldUpdate = !session.LastAccessAt.HasValue || 
                                          (DateTime.Now - session.LastAccessAt.Value).TotalMinutes > 5;
                        
                        if (shouldUpdate)
                        {
                            session.LastAccessAt = DateTime.Now;
                            dbContext.UserSessions.Update(session);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error nh?ng KHÔNG block request (?? tránh DOS)
                    _logger.LogError(ex, "Error validating token session");
                }
            }

            // ? Ti?p t?c request pipeline
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method ?? d? dàng register middleware
    /// </summary>
    public static class TokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        }
    }
}
