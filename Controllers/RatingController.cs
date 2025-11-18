using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using HUIT_Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    /// <summary>
    /// API Controller cho quản lý đánh giá
    /// </summary>
  [Authorize]
    [ApiController]
  [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
     private readonly IRatingService _ratingService;
     private readonly ILogger<RatingController> _logger;
        private readonly HuitThuVienContext _context;

        public RatingController(
       IRatingService ratingService, 
            ILogger<RatingController> logger,
            HuitThuVienContext context)
        {
  _ratingService = ratingService;
      _logger = logger;
    _context = context;
        }

    private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
    throw new UnauthorizedAccessException("Không thể xác định người dùng.");
      }
            return userId;
        }

        /// <summary>
        /// Tạo đánh giá mới
    /// </summary>
  [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingRequest request)
        {
   try
            {
              var userId = GetCurrentUserId();
          var result = await _ratingService.CreateRatingAsync(userId, request);

   if (result.Success)
     {
         return Ok(new
  {
              success = true,
            message = result.Message,
      maDanhGia = result.MaDanhGia
  });
           }

     return BadRequest(new { success = false, message = result.Message });
         }
    catch (UnauthorizedAccessException ex)
            {
       _logger.LogWarning(ex, "Unauthorized access in CreateRating");
     return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
        catch (Exception ex)
      {
         _logger.LogError(ex, "Error in CreateRating");
 return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
  /// Cập nhật đánh giá (chỉ trong 1 tuần sau khi trả phòng)
        /// </summary>
        [HttpPut("{maDanhGia}")]
      public async Task<IActionResult> UpdateRating(int maDanhGia, [FromBody] UpdateRatingRequest request)
        {
   try
     {
 var userId = GetCurrentUserId();
                var result = await _ratingService.UpdateRatingAsync(userId, maDanhGia, request);

              return result.Success ?
      Ok(new { success = true, message = result.Message }) :
  BadRequest(new { success = false, message = result.Message });
}
        catch (UnauthorizedAccessException ex)
 {
                _logger.LogWarning(ex, "Unauthorized access in UpdateRating");
       return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
       }
            catch (Exception ex)
   {
    _logger.LogError(ex, "Error in UpdateRating for rating {MaDanhGia}", maDanhGia);
    return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
   /// Lấy danh sách đánh giá của người dùng hiện tại
        /// </summary>
        [HttpGet("my-ratings")]
        public async Task<IActionResult> GetMyRatings([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
         {
     var userId = GetCurrentUserId();
 var result = await _ratingService.GetUserRatingsAsync(userId, pageNumber, pageSize);

                return Ok(new
       {
  success = true,
          data = result,
       total = result.Count,
         pageNumber = pageNumber,
              pageSize = pageSize
       });
  }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in GetMyRatings");
    return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
         }
         catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyRatings");
        return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// Lấy đánh giá theo đối tượng (phòng, dịch vụ, nhân viên)
  /// </summary>
        [HttpGet("object/{loaiDoiTuong}/{maDoiTuong}")]
        [AllowAnonymous] // Cho phép khách xem đánh giá
     public async Task<IActionResult> GetRatingsByObject(string loaiDoiTuong, int maDoiTuong, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
 try
    {
        var result = await _ratingService.GetRatingsByObjectAsync(loaiDoiTuong, maDoiTuong, pageNumber, pageSize);
     return Ok(new { success = true, data = result });
            }
       catch (Exception ex)
        {
          _logger.LogError(ex, "Error in GetRatingsByObject for {ObjectType}-{ObjectId}", loaiDoiTuong, maDoiTuong);
         return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
     }
        }

    /// <summary>
   /// Lấy thống kê đánh giá của đối tượng
    /// </summary>
        [HttpGet("statistics/{loaiDoiTuong}/{maDoiTuong}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRatingStatistics(string loaiDoiTuong, int maDoiTuong)
        {
       try
            {
    var result = await _ratingService.GetRatingStatisticsAsync(loaiDoiTuong, maDoiTuong);

     if (result == null)
        return NotFound(new { success = false, message = "Không tìm thấy thống kê đánh giá." });

            return Ok(new { success = true, data = result });
 }
   catch (Exception ex)
  {
    _logger.LogError(ex, "Error in GetRatingStatistics for {ObjectType}-{ObjectId}", loaiDoiTuong, maDoiTuong);
         return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// Tìm kiếm đánh giá
    /// </summary>
      [HttpPost("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchRatings([FromBody] RatingFilterRequest filter, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
  try
            {
    var result = await _ratingService.SearchRatingsAsync(filter, pageNumber, pageSize);
    return Ok(new { success = true, data = result });
            }
       catch (Exception ex)
          {
     _logger.LogError(ex, "Error in SearchRatings");
   return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
    }
   }

        /// <summary>
     /// Lấy chi tiết một đánh giá
      /// </summary>
[HttpGet("{maDanhGia}")]
    [AllowAnonymous]
        public async Task<IActionResult> GetRatingDetail(int maDanhGia)
        {
     try
    {
       var result = await _ratingService.GetRatingDetailAsync(maDanhGia);

       if (result == null)
        return NotFound(new { success = false, message = "Không tìm thấy đánh giá." });

     return Ok(new { success = true, data = result });
    }
      catch (Exception ex)
        {
       _logger.LogError(ex, "Error in GetRatingDetail for rating {MaDanhGia}", maDanhGia);
    return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
         }
 }

        /// <summary>
        /// Kiểm tra quyền đánh giá (chỉ trong 1 tuần sau khi trả phòng)
  /// </summary>
        [HttpGet("can-rate/{loaiDoiTuong}/{maDoiTuong}")]
  public async Task<IActionResult> CanUserRate(string loaiDoiTuong, int maDoiTuong, [FromQuery] int? maDangKy = null)
 {
     try
       {
      var userId = GetCurrentUserId();
        var result = await _ratingService.CanUserRateAsync(userId, loaiDoiTuong, maDoiTuong, maDangKy);

  return Ok(new
                {
          success = true,
          canRate = result.CanRate,
         message = result.Message
});
     }
    catch (UnauthorizedAccessException ex)
  {
            _logger.LogWarning(ex, "Unauthorized access in CanUserRate");
            return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
   }
            catch (Exception ex)
            {
           _logger.LogError(ex, "Error in CanUserRate for {ObjectType}-{ObjectId}", loaiDoiTuong, maDoiTuong);
     return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
        /// API đặc biệt cho đánh giá phòng sau khi trả phòng (chỉ trong 1 tuần)
        /// </summary>
        [HttpPost("rate-room")]
        public async Task<IActionResult> RateRoom([FromBody] CreateRatingRequest request)
    {
  try
         {
  var userId = GetCurrentUserId();

    // Ép buộc loại đối tượng là PHONG
             request.LoaiDoiTuong = "PHONG";

    var result = await _ratingService.CreateRatingAsync(userId, request);

          if (result.Success)
        {
        return Ok(new
           {
     success = true,
    message = "Cảm ơn bạn đã đánh giá! Ý kiến của bạn giúp chúng tôi cải thiện chất lượng dịch vụ.",
     maDanhGia = result.MaDanhGia
          });
                }

    return BadRequest(new { success = false, message = result.Message });
          }
  catch (UnauthorizedAccessException ex)
            {
        _logger.LogWarning(ex, "Unauthorized access in RateRoom");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
     }
        catch (Exception ex)
        {
_logger.LogError(ex, "Error in RateRoom");
        return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        /// <summary>
     /// API lấy thông tin đánh giá theo booking
        /// </summary>
        [HttpGet("booking-rating/{maDangKy}")]
        public async Task<IActionResult> GetBookingRating(int maDangKy)
     {
  try
  {
   var userId = GetCurrentUserId();

                // Kiểm tra booking có thuộc về user này không
             var booking = await _context.DangKyPhongs
         .FirstOrDefaultAsync(dk => dk.MaDangKy == maDangKy && dk.MaNguoiDung == userId);

      if (booking == null)
    return NotFound(new { success = false, message = "Không tìm thấy booking." });

    // Tìm đánh giá
                var rating = await (from r in _context.DanhGia
          join u in _context.NguoiDungs on r.MaNguoiDung equals u.MaNguoiDung
          join p in _context.Phongs on r.MaPhong equals p.MaPhong
        where r.MaDangKy == maDangKy && r.MaNguoiDung == userId
        select new
            {
      MaDanhGia = r.MaDanhGia,
    DiemDanhGia = r.DiemDanhGia,
    NoiDung = r.NoiDung,
         NgayDanhGia = r.NgayDanhGia,
              TenPhong = p.TenPhong,
          TenNguoiDung = u.HoTen
           }).FirstOrDefaultAsync();

       if (rating == null)
      {
        // Kiểm tra có thể đánh giá không
  var canRate = await _ratingService.CanUserRateAsync(userId, "PHONG", booking.MaPhong ?? 0, maDangKy);

  return Ok(new
       {
 success = true,
        hasRating = false,
          canRate = canRate.CanRate,
        message = canRate.Message,
      bookingInfo = new
     {
            maDangKy = booking.MaDangKy,
            tenPhong = booking.MaPhongNavigation?.TenPhong ?? "Không xác định",
thoiGianSuDung = booking.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm")
     }
   });
        }

    return Ok(new
      {
             success = true,
         hasRating = true,
    rating = new
        {
     maDanhGia = rating.MaDanhGia,
   diemDanhGia = rating.DiemDanhGia,
      noiDung = rating.NoiDung,
          ngayDanhGia = rating.NgayDanhGia,
      tenPhong = rating.TenPhong,
        tenNguoiDung = rating.TenNguoiDung
    }
       });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in GetBookingRating");
                return Unauthorized(new { success = false, message = "Không có quyền truy cập." });
            }
       catch (Exception ex)
          {
       _logger.LogError(ex, "Error in GetBookingRating for booking {MaDangKy}", maDangKy);
   return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi không mong muốn." });
    }
        }
    }
}