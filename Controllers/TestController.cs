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
        /// API cho t?t c? user ?ã ??ng nh?p
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
                Message = "B?n ?ã truy c?p thành công API ???c b?o v?!",
                UserId = userId,
                UserName = userName,
                Role = userRole
            });
        }

        /// <summary>
        /// API ch? cho sinh viên
        /// </summary>
        [RoleAuthorize("SINH_VIEN")]
        [HttpGet("students-only")]
        public IActionResult GetStudentsOnly()
        {
            return Ok(new { Message = "Ch? sinh viên m?i truy c?p ???c API này!" });
        }

        /// <summary>
        /// API ch? cho gi?ng viên
        /// </summary>
        [RoleAuthorize("GIANG_VIEN")]
        [HttpGet("teachers-only")]
        public IActionResult GetTeachersOnly()
        {
            return Ok(new { Message = "Ch? gi?ng viên m?i truy c?p ???c API này!" });
        }

        /// <summary>
        /// API cho c? sinh viên và gi?ng viên
        /// </summary>
        [RoleAuthorize("SINH_VIEN", "GIANG_VIEN")]
        [HttpGet("users-only")]
        public IActionResult GetUsersOnly()
        {
            return Ok(new { Message = "API dành cho sinh viên và gi?ng viên!" });
        }

        /// <summary>
        /// API ch? cho admin
        /// </summary>
        [RoleAuthorize("QUAN_TRI")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminOnly()
        {
            return Ok(new { Message = "Ch? qu?n tr? viên m?i truy c?p ???c API này!" });
        }

        /// <summary>
        /// API cho admin và nhân viên
        /// </summary>
        [RoleAuthorize("QUAN_TRI", "NHAN_VIEN")]
        [HttpGet("management-only")]
        public IActionResult GetManagementOnly()
        {
            return Ok(new { Message = "API dành cho qu?n lý (Admin và nhân viên)!" });
        }
    }
}