using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HUIT_Library.Controllers
{
  /// <summary>
    /// API Controller cho tìm ki?m phòng tr?ng
    /// </summary>
 [ApiController]
    [Route("api/[controller]")]
    public class AvailableRoomController : ControllerBase
    {
private readonly IAvailableRoomService _availableRoomService;
        private readonly ILogger<AvailableRoomController> _logger;

      public AvailableRoomController(IAvailableRoomService availableRoomService, ILogger<AvailableRoomController> logger)
        {
    _availableRoomService = availableRoomService;
            _logger = logger;
        }

    /// <summary>
  /// ?? Tìm ki?m phòng tr?ng theo th?i gian và lo?i phòng
        /// </summary>
       /// <param name="request">Thông tin tìm ki?m</param>
       /// <returns>Danh sách phòng tr?ng</returns>
        [HttpPost("search")]
  public async Task<IActionResult> FindAvailableRooms([FromBody] FindAvailableRoomRequest request)
   {
    try
     {
      // Validation
         if (request.MaLoaiPhong <= 0)
  {
      return BadRequest(new 
    { 
          success = false, 
 message = "Vui lòng ch?n lo?i phòng h?p l?." 
    });
}

       if (request.ThoiGianBatDau == default)
     {
   return BadRequest(new 
  { 
    success = false, 
    message = "Vui lòng ch?n th?i gian b?t ??u h?p l?." 
   });
       }

 if (request.ThoiGianSuDung <= 0 || request.ThoiGianSuDung > 8)
       {
             return BadRequest(new 
    { 
    success = false, 
       message = "Th?i gian s? d?ng ph?i t? 1 ??n 8 gi?." 
           });
   }

   // Ki?m tra th?i gian b?t ??u không ???c trong quá kh?
       if (request.ThoiGianBatDau < DateTime.Now.AddMinutes(-30))
      {
       return BadRequest(new 
     { 
   success = false, 
        message = "Th?i gian b?t ??u không th? trong quá kh?." 
      });
  }

 var results = await _availableRoomService.FindAvailableRoomsAsync(request);

     if (!results.Any())
       {
       return Ok(new 
     { 
        success = true,
   message = $"Không có phòng tr?ng cho lo?i phòng này t? {request.ThoiGianBatDau:dd/MM/yyyy HH:mm} trong {request.ThoiGianSuDung} gi?.",
       data = new List<AvailableRoomDto>(),
              total = 0
       });
      }

          var thoiGianKetThuc = request.ThoiGianBatDau.AddHours(request.ThoiGianSuDung);

     return Ok(new 
      { 
        success = true,
       message = $"Tìm th?y {results.Count} phòng tr?ng t? {request.ThoiGianBatDau:dd/MM/yyyy HH:mm} ??n {thoiGianKetThuc:dd/MM/yyyy HH:mm}.",
      data = results,
          total = results.Count
   });
        }
      catch (Exception ex)
  {
 _logger.LogError(ex, "Error finding available rooms");
        return StatusCode(500, new 
        { 
           success = false, 
             message = "?ã x?y ra l?i khi tìm ki?m phòng tr?ng." 
  });
      }
        }

    /// <summary>
 /// ? Ki?m tra phòng c? th? có tr?ng không
  /// </summary>
      /// <param name="maPhong">Mã phòng</param>
    /// <param name="thoiGianBatDau">Th?i gian b?t ??u (yyyy-MM-dd HH:mm)</param>
    /// <param name="thoiGianSuDung">Th?i gian s? d?ng (gi?)</param>
        [HttpGet("check/{maPhong}")]
     public async Task<IActionResult> CheckRoomAvailability(
      int maPhong, 
            [FromQuery] DateTime thoiGianBatDau, 
  [FromQuery] int thoiGianSuDung = 2)
   {
          try
  {
 if (thoiGianSuDung <= 0 || thoiGianSuDung > 8)
    {
           return BadRequest(new 
 { 
        success = false, 
        message = "Th?i gian s? d?ng ph?i t? 1 ??n 8 gi?." 
       });
       }

     var thoiGianKetThuc = thoiGianBatDau.AddHours(thoiGianSuDung);
      var isAvailable = await _availableRoomService.IsRoomAvailableAsync(maPhong, thoiGianBatDau, thoiGianKetThuc);

            return Ok(new 
       { 
    success = true,
         data = new 
    { 
           MaPhong = maPhong,
             ThoiGianBatDau = thoiGianBatDau,
          ThoiGianKetThuc = thoiGianKetThuc,
      IsAvailable = isAvailable
           },
   message = isAvailable ? "Phòng có s?n trong th?i gian này." : "Phòng ?ã ???c ??t trong th?i gian này."
  });
   }
   catch (Exception ex)
     {
         _logger.LogError(ex, "Error checking room availability for room {RoomId}", maPhong);
       return StatusCode(500, new 
 { 
     success = false, 
     message = "?ã x?y ra l?i khi ki?m tra tình tr?ng phòng." 
   });
      }
        }

        /// <summary>
 /// ?? L?y danh sách lo?i phòng
     /// </summary>
       [HttpGet("room-types")]
    public async Task<IActionResult> GetRoomTypes()
 {
  try
 {
     var roomTypes = await _availableRoomService.GetRoomTypesAsync();
      
      return Ok(new 
    { 
       success = true,
  data = roomTypes,
       total = roomTypes.Count,
     message = $"L?y thành công {roomTypes.Count} lo?i phòng."
 });
   }
catch (Exception ex)
     {
     _logger.LogError(ex, "Error getting room types");
  return StatusCode(500, new 
            { 
  success = false, 
    message = "?ã x?y ra l?i khi l?y danh sách lo?i phòng." 
      });
        }
        }

        /// <summary>
    /// ?? API h? tr?: Tìm ki?m nhanh v?i tham s? URL
        /// </summary>
        /// <param name="maLoaiPhong">Mã lo?i phòng</param>
/// <param name="thoiGianBatDau">Th?i gian b?t ??u (yyyy-MM-dd HH:mm)</param>
      /// <param name="thoiGianSuDung">Th?i gian s? d?ng (gi?) - m?c ??nh 2</param>
        /// <param name="sucChuaToiThieu">S?c ch?a t?i thi?u (tùy ch?n)</param>
        [HttpGet("quick-search")]
        public async Task<IActionResult> QuickSearch(
    [FromQuery] int maLoaiPhong,
     [FromQuery] DateTime thoiGianBatDau,
   [FromQuery] int thoiGianSuDung = 2,
        [FromQuery] int? sucChuaToiThieu = null)
        {
         try
            {
    var request = new FindAvailableRoomRequest
           {
 MaLoaiPhong = maLoaiPhong,
          ThoiGianBatDau = thoiGianBatDau,
         ThoiGianSuDung = thoiGianSuDung,
   SucChuaToiThieu = sucChuaToiThieu
     };

      return await FindAvailableRooms(request);
    }
  catch (Exception ex)
     {
  _logger.LogError(ex, "Error in quick search");
     return StatusCode(500, new 
   { 
         success = false, 
           message = "?ã x?y ra l?i khi tìm ki?m nhanh." 
    });
   }
   }
    }
}