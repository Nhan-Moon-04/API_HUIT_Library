using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoaiPhongController : Controller
    {
        private readonly ILoaiPhongServices _loaiphong;

        public LoaiPhongController(ILoaiPhongServices loaiphong)
        {
            _loaiphong = loaiphong;
        }
        
        [NonAction]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [Route("getall")]
        public async Task<IActionResult> GetAll()   
        {

            var loaiPhongs = await _loaiphong.GetAllLoaiPhong();
            return Ok(loaiPhongs);
        }
    }
}
