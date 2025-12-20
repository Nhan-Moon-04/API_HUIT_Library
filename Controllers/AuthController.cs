using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        /// <summary>
        /// Đăng nhập cho sinh viên và giảng viên (UI)
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ!"
                });
            }

            // ✅ Tự động lấy IP Address từ request
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            
            // ✅ Tự động lấy Device Info từ User-Agent header
            var userAgent = Request.Headers["User-Agent"].ToString();
            var deviceInfo = string.IsNullOrEmpty(userAgent) ? "Unknown Device" : ParseUserAgent(userAgent);
            
            // Ghi đè thông tin tự động detect (ưu tiên server-side)
            request.DeviceInfo = deviceInfo;
            request.IpAddress = ipAddress;

            var result = await _authService.LoginAsync(request);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Parse User-Agent header để lấy thông tin thiết bị
        /// </summary>
        private string ParseUserAgent(string userAgent)
        {
            try
            {
                // Detect Browser
                string browser = "Unknown Browser";
                if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
                    browser = "Chrome";
                else if (userAgent.Contains("Edg"))
                    browser = "Edge";
                else if (userAgent.Contains("Firefox"))
                    browser = "Firefox";
                else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
                    browser = "Safari";
                else if (userAgent.Contains("Opera") || userAgent.Contains("OPR"))
                    browser = "Opera";

                // Detect OS
                string os = "Unknown OS";
                if (userAgent.Contains("Windows NT 10.0"))
                    os = "Windows 10/11";
                else if (userAgent.Contains("Windows NT 6.3"))
                    os = "Windows 8.1";
                else if (userAgent.Contains("Windows NT 6.2"))
                    os = "Windows 8";
                else if (userAgent.Contains("Windows NT 6.1"))
                    os = "Windows 7";
                else if (userAgent.Contains("Mac OS X"))
                    os = "macOS";
                else if (userAgent.Contains("Linux"))
                    os = "Linux";
                else if (userAgent.Contains("Android"))
                    os = "Android";
                else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                    os = "iOS";

                return $"{browser} on {os}";
            }
            catch
            {
                return "Unknown Device";
            }
        }

        /// <summary>
        /// ✅ Lấy danh sách thiết bị đang đăng nhập
        /// </summary>
        [Authorize]
        [HttpGet("sessions")]
        public async Task<ActionResult<ActiveSessionsResponse>> GetActiveSessions()
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { 
                        message = "Vui lòng đăng nhập để xem thiết bị",
                        hint = "Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại."
                    });
                }

                // Lấy sessionId từ token
                var sessionIdClaim = User.FindFirst("SessionId")?.Value;
                var currentSessionId = 0;
                if (!string.IsNullOrEmpty(sessionIdClaim))
                {
                    int.TryParse(sessionIdClaim, out currentSessionId);
                }

                var result = await _authService.GetActiveSessionsAsync(userId, currentSessionId);
                
                if (result.Success)
                    return Ok(result);
                    
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống khi lấy danh sách thiết bị",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// ✅ Đăng xuất một thiết bị cụ thể
        /// </summary>
        [Authorize]
        [HttpPost("sessions/{sessionId}/logout")]
        public async Task<IActionResult> LogoutSession(int sessionId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Vui lòng đăng nhập" });
                }

                var (success, message) = await _authService.LogoutSessionAsync(userId, sessionId);
                
                if (success)
                    return Ok(new { success = true, message });
                    
                return BadRequest(new { success = false, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống khi đăng xuất thiết bị",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// ✅ Đăng xuất tất cả thiết bị khác (giữ lại thiết bị hiện tại)
        /// </summary>
        [Authorize]
        [HttpPost("sessions/logout-others")]
        public async Task<IActionResult> LogoutOtherSessions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Vui lòng đăng nhập" });
                }

                // Lấy sessionId hiện tại từ token
                var sessionIdClaim = User.FindFirst("SessionId")?.Value;
                if (string.IsNullOrEmpty(sessionIdClaim) || !int.TryParse(sessionIdClaim, out var currentSessionId))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Không xác định được phiên đăng nhập hiện tại" 
                    });
                }

                var (success, message) = await _authService.LogoutOtherSessionsAsync(userId, currentSessionId);
                
                if (success)
                    return Ok(new { success = true, message });
                    
                return BadRequest(new { success = false, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống khi đăng xuất các thiết bị khác",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// ??ng ký cho sinh viên và gi?ng viên
        /// </summary>



        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request.Email);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // Only return token in Development environment for testing
            var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
            var tokenForResponse = env == "Development" ? result.Token : null;

            return Ok(new { message = result.Message, emailSent = result.EmailSent, token = tokenForResponse });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result)
                return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

            return Ok(new { message = "Đặt lại mật khẩu thành công!" });
        }

        // Development-only: retrieve latest reset token for an email
        [HttpGet("dev/latest-reset-token")]
        public async Task<IActionResult> GetLatestResetToken([FromQuery] string email)
        {
            var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
            if (env != "Development")
                return NotFound();

            var token = await _authService.GetLatestResetTokenByEmailAsync(email);
            if (token == null)
                return NotFound(new { message = "Không tìm thấy user hoặc token." });

            return Ok(new { email, token });
        }

        // Allow calling change-password either with a valid JWT or by supplying MaCode in the request body (for Swagger/testing)
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Prefer authenticated claim, otherwise fall back to the request body MaCode
            var maDangNhap = User.FindFirst("MaCode")?.Value ?? request.MaCode;

            if (string.IsNullOrEmpty(maDangNhap))
            {
                // Neither authenticated nor provided MaCode
                return Unauthorized(new { message = "Authentication required or provide MaCode in request body." });
            }

            var result = await _authService.ChangePasswordAsync(maDangNhap, request.CurrentPassword, request.NewPassword);

            if (!result)
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

     
    }
}