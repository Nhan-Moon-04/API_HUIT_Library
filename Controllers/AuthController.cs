using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services;
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

    }
}