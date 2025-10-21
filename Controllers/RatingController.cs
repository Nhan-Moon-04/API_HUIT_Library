using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
        private readonly HuitThuVienContext _context;

        public RatingController(HuitThuVienContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitRating([FromBody] SubmitRatingRequest request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // Validate scores (1-5)
            int? Normalize(int? v) => v.HasValue && v >= 1 && v <= 5 ? v : null;
            var roomScore = Normalize(request.RoomScore);
            var serviceScore = Normalize(request.ServiceScore);
            var staffScore = Normalize(request.StaffScore);

            if (!roomScore.HasValue && !serviceScore.HasValue && !staffScore.HasValue)
                return BadRequest(new { message = "Vui lòng g?i ít nh?t m?t tiêu chí ?ánh giá (room/service/staff) v?i ?i?m 1-5." });

            var now = DateTime.UtcNow;
            var created = new List<DanhGiaTv>();

            if (roomScore.HasValue)
            {
                var r = new DanhGiaTv
                {
                    MaNguoiDung = userId,
                    LoaiDoiTuong = "Phong",
                    MaDoiTuong = request.MaPhong,
                    DiemDanhGia = roomScore,
                    NoiDung = request.NoiDung,
                    NgayDanhGia = now
                };
                _context.DanhGiaTvs.Add(r);
                created.Add(r);
            }

            if (serviceScore.HasValue)
            {
                // Service review associated with booking if provided, otherwise associate with room
                var target = request.MaDangKy != 0 ? request.MaDangKy : request.MaPhong;
                var r = new DanhGiaTv
                {
                    MaNguoiDung = userId,
                    LoaiDoiTuong = "DichVu",
                    MaDoiTuong = target,
                    DiemDanhGia = serviceScore,
                    NoiDung = request.NoiDung,
                    NgayDanhGia = now
                };
                _context.DanhGiaTvs.Add(r);
                created.Add(r);
            }

            if (staffScore.HasValue && request.MaNhanVien.HasValue)
            {
                var r = new DanhGiaTv
                {
                    MaNguoiDung = userId,
                    LoaiDoiTuong = "NhanVien",
                    MaDoiTuong = request.MaNhanVien.Value,
                    DiemDanhGia = staffScore,
                    NoiDung = request.NoiDung,
                    NgayDanhGia = now
                };
                _context.DanhGiaTvs.Add(r);
                created.Add(r);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "L?i khi l?u ?ánh giá.", detail = ex.Message });
            }

            return Ok(new { message = "?ã g?i ?ánh giá thành công.", count = created.Count, createdIds = created.Select(c => c.MaDanhGia) });
        }
    }
}
