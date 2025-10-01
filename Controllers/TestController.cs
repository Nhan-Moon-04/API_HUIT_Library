using HUIT_Library.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// API cho t?t c? user ?� ??ng nh?p
        /// </summary>
        [Authorize]
        [HttpGet("protected")]
        public IActionResult GetProtected()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            return Ok(new
            {
                Message = "B?n ?� truy c?p th�nh c�ng API ???c b?o v?!",
                UserId = userId,
                UserName = userName,
                Role = userRole
            });
        }

        /// <summary>
        /// API ch? cho sinh vi�n
        /// </summary>
        [RoleAuthorize("SINH_VIEN")]
        [HttpGet("students-only")]
        public IActionResult GetStudentsOnly()
        {
            return Ok(new { Message = "Ch? sinh vi�n m?i truy c?p ???c API n�y!" });
        }

        /// <summary>
        /// API ch? cho gi?ng vi�n
        /// </summary>
        [RoleAuthorize("GIANG_VIEN")]
        [HttpGet("teachers-only")]
        public IActionResult GetTeachersOnly()
        {
            return Ok(new { Message = "Ch? gi?ng vi�n m?i truy c?p ???c API n�y!" });
        }

        /// <summary>
        /// API cho c? sinh vi�n v� gi?ng vi�n
        /// </summary>
        [RoleAuthorize("SINH_VIEN", "GIANG_VIEN")]
        [HttpGet("users-only")]
        public IActionResult GetUsersOnly()
        {
            return Ok(new { Message = "API d�nh cho sinh vi�n v� gi?ng vi�n!" });
        }

        /// <summary>
        /// API ch? cho admin
        /// </summary>
        [RoleAuthorize("QUAN_TRI")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminOnly()
        {
            return Ok(new { Message = "Ch? qu?n tr? vi�n m?i truy c?p ???c API n�y!" });
        }

        /// <summary>
        /// API cho admin v� nh�n vi�n
        /// </summary>
        [RoleAuthorize("QUAN_TRI", "NHAN_VIEN")]
        [HttpGet("management-only")]
        public IActionResult GetManagementOnly()
        {
            return Ok(new { Message = "API d�nh cho qu?n l� (Admin v� nh�n vi�n)!" });
        }
    }
}