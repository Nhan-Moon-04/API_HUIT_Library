using HUIT_Library.Attributes;
using HUIT_Library.Models;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilityController : ControllerBase
    {
        private readonly HuitThuVienContext _context;
        private readonly IPasswordHashService _passwordHashService;

        public UtilityController(HuitThuVienContext context, IPasswordHashService passwordHashService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
        }

        /// <summary>
        /// Migrate existing plain text passwords to hashed passwords
        /// Ch? admin m?i c� th? th?c hi?n
        /// </summary>
        [RoleAuthorize("QUAN_TRI")]
        [HttpPost("migrate-passwords")]
        public async Task<IActionResult> MigratePasswords()
        {
            try
            {
                // L?y t?t c? users c� plain text password (kh�ng ch?a d?u ch?m - kh�ng ph?i hash format)
                var usersWithPlainTextPasswords = await _context.NguoiDungs
                    .Where(u => u.MatKhau != null && !u.MatKhau.Contains("."))
                    .ToListAsync();

                var migratedCount = 0;

                foreach (var user in usersWithPlainTextPasswords)
                {
                    // Hash password c?
                    var hashedPassword = _passwordHashService.HashPassword(user.MatKhau!);
                    user.MatKhau = hashedPassword;
                    migratedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"?� migration {migratedCount} m?t kh?u th�nh c�ng!",
                    MigratedCount = migratedCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"L?i migration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Test password hashing - ch? ?? development
        /// </summary>
        [HttpPost("test-hash")]
        public IActionResult TestHash([FromBody] TestHashRequest request)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Password is required");
            }

            var hashedPassword = _passwordHashService.HashPassword(request.Password);
            var isValid = _passwordHashService.VerifyPassword(request.Password, hashedPassword);

            return Ok(new
            {
                OriginalPassword = request.Password,
                HashedPassword = hashedPassword,
                VerificationResult = isValid,
                Message = isValid ? "Hash v� verify th�nh c�ng!" : "C� l?i trong qu� tr�nh hash/verify"
            });
        }

        /// <summary>
        /// Reset password cho user - ch? admin
        /// </summary>
        [RoleAuthorize("QUAN_TRI")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.NguoiDungs
                    .FirstOrDefaultAsync(u => u.MaCode == request.UserCode);

                if (user == null)
                {
                    return NotFound(new { Message = "Kh�ng t�m th?y user!" });
                }

                // Hash new password
                var hashedPassword = _passwordHashService.HashPassword(request.NewPassword);
                user.MatKhau = hashedPassword;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"?� reset m?t kh?u cho user {user.HoTen} th�nh c�ng!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"L?i reset password: {ex.Message}"
                });
            }
        }
    }

    public class TestHashRequest
    {
        public string Password { get; set; } = null!;
    }

    public class ResetPasswordRequest
    {
        public string UserCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}