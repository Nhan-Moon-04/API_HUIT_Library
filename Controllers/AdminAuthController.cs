using HUIT_Library.DTOs;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// ??ng nh?p cho qu?n tr? viên và nhân viên (WinForm)
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> AdminLogin([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "D? li?u không h?p l?!"
                });
            }

            var result = await _authService.AdminLoginAsync(request);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }
    }
}