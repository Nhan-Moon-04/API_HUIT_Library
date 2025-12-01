using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// ??ng nh?p cho sinh viên và gi?ng viên (UI)
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "D? li?u không h?p l?!"
                });
            }

            var result = await _authService.LoginAsync(request);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
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

        /// <summary>
        /// 🔧 DEVELOPMENT ONLY: Hash tất cả password plain text trong database
     /// </summary>
        [HttpPost("dev/hash-all-passwords")]
  public async Task<IActionResult> HashAllPlainTextPasswords()
        {
       var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
   if (env != "Development")
         return NotFound();

       var (updatedCount, message) = await _authService.HashAllPlainTextPasswordsAsync();
   
   return Ok(new { 
  message, 
   updatedCount,
   timestamp = DateTime.UtcNow
        });
 }

        /// <summary>
        /// ✅ Đăng nhập cho admin (Quản lý kỹ thuật, Quản lý thư viện, Nhân viên thư viên)
        /// Tự động tạo token vĩnh viễn cho vai trò 1, 2, 3
        /// </summary>
        [HttpPost("admin-login")]
    public async Task<ActionResult<LoginResponse>> AdminLogin([FromBody] LoginRequest request)
        {
     if (!ModelState.IsValid)
  {
                return BadRequest(new LoginResponse
     {
         Success = false,
      Message = "Dữ liệu không hợp lệ!"
  });
    }

            var result = await _authService.AdminLoginAsync(request);
 
            if (result.Success)
          {
      // Add additional info about token type for admin
   return Ok(new 
       {
         result.Success,
 result.Message,
      result.Token,
         result.User,
             TokenInfo = new 
{
             Type = result.User?.VaiTro switch 
  {
 "QUAN_TRI" => "Permanent (Vĩnh viễn)",
      "NHAN_VIEN" => result.User.MaNguoiDung <= 3 ? "Permanent (Vĩnh viễn)" : "Temporary (7 ngày)",
    _ => "Temporary (7 ngày)"
  },
           ExpiresIn = result.User?.VaiTro == "QUAN_TRI" || (result.User?.MaNguoiDung <= 3) ? "Never" : "7 days",
                  IssuedAt = DateTime.UtcNow
    }
   });
}
        
  return BadRequest(result);
        }

        /// <summary>
        /// ✅ Test endpoint để kiểm tra token vĩnh viễn
        /// </summary>
        [Authorize]
        [HttpGet("test-permanent-token")]
  public IActionResult TestPermanentToken()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
   var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
   var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var tokenType = User.FindFirst("TokenType")?.Value;
          var issuedAt = User.FindFirst("IssuedAt")?.Value;

       return Ok(new
            {
        message = "Token hoạt động tốt!",
    currentTime = DateTime.UtcNow,
                user = new
           {
        id = userId,
        name = userName,
 role = userRole,
      tokenType = tokenType ?? "Unknown"
                },
           tokenInfo = new
   {
         type = tokenType,
            issuedAt = issuedAt != null ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAt)).DateTime : (DateTime?)null,
 isPermanent = tokenType == "Permanent",
        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
    }
 });
        }
    }
}