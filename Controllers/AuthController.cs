using HUIT_Library.DTOs;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            // Debug logging
            Console.WriteLine($"CONTROLLER DEBUG - VaiTro: {request?.VaiTro ?? "NULL"}");
            Console.WriteLine($"CONTROLLER DEBUG - MaSinhVien: {request?.MaSinhVien ?? "NULL"}");
            Console.WriteLine($"CONTROLLER DEBUG - MaNhanVien: {request?.MaNhanVien ?? "NULL"}");
            Console.WriteLine($"CONTROLLER DEBUG - HoTen: {request?.HoTen ?? "NULL"}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("CONTROLLER DEBUG - ModelState Invalid:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "D? li?u không h?p l?!"
                });
            }

            var result = await _authService.RegisterAsync(request);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }
    }
}