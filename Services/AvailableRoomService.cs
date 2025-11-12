using Dapper;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace HUIT_Library.Services
{
    public class AvailableRoomService : IAvailableRoomService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<AvailableRoomService> _logger;

    // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
 return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public AvailableRoomService(HuitThuVienContext context, ILogger<AvailableRoomService> logger)
   {
   _context = context;
            _logger = logger;
        }

   public async Task<List<AvailableRoomDto>> FindAvailableRoomsAsync(FindAvailableRoomRequest request)
     {
    try
            {
          _logger.LogInformation("Finding available rooms for room type {RoomTypeId} from {StartTime} for {Duration} hours",
          request.MaLoaiPhong, request.ThoiGianBatDau, request.ThoiGianSuDung);

      // Tính th?i gian k?t thúc (m?c ??nh 2 gi?)
                var thoiGianKetThuc = request.ThoiGianBatDau.AddHours(request.ThoiGianSuDung);

           using var conn = _context.Database.GetDbConnection();
          if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

    var sql = @"
     SELECT DISTINCT 
      p.MaPhong,
             p.TenPhong,
lp.TenLoaiPhong,
            lp.SoLuongChoNgoi AS SucChua,
    p.ViTri,
                 p.MoTa,
       @thoiGianBatDau AS ThoiGianBatDau,
              @thoiGianKetThuc AS ThoiGianKetThuc,
          1 as IsAvailable
  FROM Phong p
         JOIN LoaiPhong lp ON p.MaLoaiPhong = lp.MaLoaiPhong
     WHERE p.TinhTrang = N'Ho?t ??ng'
AND lp.MaLoaiPhong = @maLoaiPhong";

             var parameters = new DynamicParameters();
  parameters.Add("@maLoaiPhong", request.MaLoaiPhong);
       parameters.Add("@thoiGianBatDau", request.ThoiGianBatDau);
    parameters.Add("@thoiGianKetThuc", thoiGianKetThuc);

     // L?c theo s?c ch?a t?i thi?u n?u có
    if (request.SucChuaToiThieu.HasValue)
     {
           sql += " AND TRY_CAST(lp.SoLuongChoNgoi AS INT) >= @sucChuaToiThieu";
  parameters.Add("@sucChuaToiThieu", request.SucChuaToiThieu.Value);
 }

      // Lo?i b? phòng ?ã ???c ??t trong kho?ng th?i gian này
 sql += @"
        AND NOT EXISTS (
       SELECT 1 FROM DangKyPhong d 
          WHERE d.MaPhong = p.MaPhong 
       AND d.MaTrangThai NOT IN (3, 5) -- Không ph?i t? ch?i ho?c h?y
       AND NOT (@thoiGianKetThuc <= d.ThoiGianBatDau OR @thoiGianBatDau >= d.ThoiGianKetThuc)
    )";

             // Lo?i b? phòng có l?ch b?o trì
         sql += @"
                    AND NOT EXISTS (
          SELECT 1 FROM LichTrangThaiPhong l 
     WHERE l.MaPhong = p.MaPhong 
       AND l.Ngay = CAST(@thoiGianBatDau AS DATE)
       AND NOT (CAST(@thoiGianKetThuc AS TIME) <= l.GioBatDau OR CAST(@thoiGianBatDau AS TIME) >= l.GioKetThuc)
 )
    ORDER BY p.TenPhong";

           var rooms = await conn.QueryAsync<AvailableRoomDto>(sql, parameters);
              var roomList = rooms.ToList();

     // L?y thi?t b? chính cho t?ng phòng
                foreach (var room in roomList)
          {
    room.ThietBiChinh = await GetMainEquipmentAsync(room.MaPhong);
       }

       _logger.LogInformation("Found {Count} available rooms for room type {RoomTypeId}", 
        roomList.Count, request.MaLoaiPhong);

    return roomList;
            }
            catch (Exception ex)
            {
           _logger.LogError(ex, "Error finding available rooms for room type {RoomTypeId}", request.MaLoaiPhong);
        return new List<AvailableRoomDto>();
            }
        }

      public async Task<bool> IsRoomAvailableAsync(int maPhong, DateTime thoiGianBatDau, DateTime thoiGianKetThuc)
        {
            try
            {
       _logger.LogInformation("Checking if room {RoomId} is available from {StartTime} to {EndTime}",
maPhong, thoiGianBatDau, thoiGianKetThuc);

          // Ki?m tra phòng có ?ang ho?t ??ng không
   var phong = await _context.Phongs.FindAsync(maPhong);
           if (phong == null || phong.TinhTrang != "Ho?t ??ng")
    return false;

     // Ki?m tra xung ??t v?i ??ng ký khác
                var hasBookingConflict = await _context.DangKyPhongs
         .Where(d => d.MaPhong == maPhong &&
     d.MaTrangThai != 3 && d.MaTrangThai != 5 && // Không ph?i t? ch?i ho?c h?y
    !(thoiGianKetThuc <= d.ThoiGianBatDau || thoiGianBatDau >= d.ThoiGianKetThuc))
              .AnyAsync();

                if (hasBookingConflict)
        return false;

      // Ki?m tra xung ??t v?i l?ch b?o trì
         var dateOnly = DateOnly.FromDateTime(thoiGianBatDau);
     var startTime = TimeOnly.FromDateTime(thoiGianBatDau);
           var endTime = TimeOnly.FromDateTime(thoiGianKetThuc);

          var hasMaintenanceConflict = await _context.LichTrangThaiPhongs
     .Where(l => l.MaPhong == maPhong &&
 l.Ngay == dateOnly &&
        !(endTime <= l.GioBatDau || startTime >= l.GioKetThuc))
    .AnyAsync();

                return !hasMaintenanceConflict;
            }
       catch (Exception ex)
         {
            _logger.LogError(ex, "Error checking room availability for room {RoomId}", maPhong);
     return false;
            }
        }

        public async Task<List<RoomTypeSimpleDto>> GetRoomTypesAsync()
  {
            try
            {
          _logger.LogInformation("Getting all room types");

    using var conn = _context.Database.GetDbConnection();
         if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

        var sql = @"
        SELECT 
      lp.MaLoaiPhong,
        lp.TenLoaiPhong,
   lp.MoTa,
      lp.SoLuongChoNgoi,
        COUNT(p.MaPhong) as SoPhongKhaDung
         FROM LoaiPhong lp
           LEFT JOIN Phong p ON lp.MaLoaiPhong = p.MaLoaiPhong AND p.TinhTrang = N'Ho?t ??ng'
    GROUP BY lp.MaLoaiPhong, lp.TenLoaiPhong, lp.MoTa, lp.SoLuongChoNgoi
            ORDER BY lp.TenLoaiPhong";

    var results = await conn.QueryAsync<RoomTypeSimpleDto>(sql);
  
     _logger.LogInformation("Retrieved {Count} room types", results.Count());
       return results.ToList();
    }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error getting room types");
     return new List<RoomTypeSimpleDto>();
       }
 }

 /// <summary>
        /// L?y danh sách thi?t b? chính c?a phòng
    /// </summary>
        private async Task<List<string>> GetMainEquipmentAsync(int maPhong)
 {
            try
 {
 using var conn = _context.Database.GetDbConnection();
     if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

var sql = @"
        SELECT TOP 3 tn.TenTaiNguyen
            FROM PhongTaiNguyen pt
             JOIN TaiNguyen tn ON pt.MaTaiNguyen = tn.MaTaiNguyen
 WHERE pt.MaPhong = @maPhong 
             AND pt.TinhTrang = N'T?t'
ORDER BY tn.TenTaiNguyen";

         var equipment = await conn.QueryAsync<string>(sql, new { maPhong });
       return equipment.ToList();
            }
catch (Exception ex)
     {
   _logger.LogWarning(ex, "Error getting equipment for room {RoomId}", maPhong);
     return new List<string>();
            }
        }
    }
}