using Microsoft.AspNetCore.Mvc;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HUIT_Library.Services.TaiNguyen.IServices;
namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaiNguyenPhongController : Controller
    {
        private readonly ITaiNguyenLoaiPhongServices _taiNguyenPhongService;
        public TaiNguyenPhongController(ITaiNguyenLoaiPhongServices taiNguyenPhongService)
        {
            _taiNguyenPhongService = taiNguyenPhongService;
        }

        [Authorize]
        [HttpGet("Get-All-TaiNguyen-Phong")]
        public async Task<IActionResult> GetAllTaiNguyenPhong()
        {
            var result = await _taiNguyenPhongService.GetAllTaiNguyenAsync();
            return Ok(result);
        }
    }
}
