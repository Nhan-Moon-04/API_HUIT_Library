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

        public AvailableRoomService(HuitThuVienContext context, ILogger<AvailableRoomService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<AvailableRoomDto>> FindAvailableRoomsAsync(FindAvailableRoomRequest request)
        {
            try
            {
                _logger.LogInformation("Finding available rooms using SP for room type {RoomTypeId} from {StartTime} for {Duration} hours",
                request.MaLoaiPhong, request.ThoiGianBatDau, request.ThoiGianSuDung);

                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

                // ✅ Sử dụng stored procedure sp_TimPhongTrong_Web
                var parameters = new DynamicParameters();
                parameters.Add("@MaLoaiPhong", request.MaLoaiPhong);
                parameters.Add("@ThoiGianBatDau", request.ThoiGianBatDau);
                parameters.Add("@KhoangThoiGian", request.ThoiGianSuDung);

                var rooms = await conn.QueryAsync<AvailableRoomFromSPDto>(
                  "dbo.sp_TimPhongTrong_Web",
            parameters,
    commandType: CommandType.StoredProcedure);

                var roomList = new List<AvailableRoomDto>();

                foreach (var room in rooms)
                {
                    // Lọc theo sức chứa tối thiểu nếu có yêu cầu
                    if (request.SucChuaToiThieu.HasValue)
                    {
                        if (int.TryParse(room.SoLuongChoNgoi, out int capacity))
                        {
                            if (capacity < request.SucChuaToiThieu.Value)
                                continue; // Bỏ qua phòng không đủ sức chứa
                        }
                    }

                    // ✅ Chỉ trả về thông tin cần thiết cho user
                    var availableRoom = new AvailableRoomDto
                    {
                        MaPhong = room.MaPhong,
                        TenPhong = room.TenPhong,
                        TenLoaiPhong = room.TenLoaiPhong,
                        SucChua = room.SoLuongChoNgoi
                    };

                    // Lấy vị trí phòng
                    await EnhanceBasicRoomInfoAsync(availableRoom);
                    roomList.Add(availableRoom);
                }

                _logger.LogInformation("Found {Count} available rooms for room type {RoomTypeId} (after filtering)",
               roomList.Count, request.MaLoaiPhong);

                return roomList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding available rooms for room type {RoomTypeId} using stored procedure", request.MaLoaiPhong);
                return new List<AvailableRoomDto>();
            }
        }

        public async Task<RoomDetailDto?> GetRoomDetailAsync(int maPhong)
        {
            try
            {
                _logger.LogInformation("Getting room detail for room {RoomId}", maPhong);

                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                // 1️⃣ Lấy thông tin chi tiết phòng
                var roomSql = @"
            SELECT 
                p.MaPhong, 
                p.TenPhong, 
                lp.TenLoaiPhong, 
                lp.SoLuongChoNgoi AS SucChua
            FROM Phong p
            JOIN LoaiPhong lp ON p.MaLoaiPhong = lp.MaLoaiPhong
            WHERE p.MaPhong = @maPhong";

                var room = await conn.QueryFirstOrDefaultAsync<RoomDetailDto>(roomSql, new { maPhong });
                if (room == null)
                {
                    _logger.LogWarning("Room {RoomId} not found", maPhong);
                    return null;
                }

                _logger.LogInformation("Found room: {RoomName} ({RoomType})", room.TenPhong, room.TenLoaiPhong);

                // 2️⃣ Lấy danh sách tài nguyên
                var resourceSql = @"
            SELECT 
                tn.TenTaiNguyen, 
                pt.SoLuong
            FROM Phong_TaiNguyen pt
            JOIN TaiNguyen tn ON pt.MaTaiNguyen = tn.MaTaiNguyen
            WHERE pt.MaPhong = @maPhong
            ORDER BY tn.TenTaiNguyen";

                var resources = await conn.QueryAsync<RoomResourceDto>(resourceSql, new { maPhong });
                room.TaiNguyen = resources.ToList();

                _logger.LogInformation("Room {RoomId} has {ResourceCount} resources", maPhong, room.TaiNguyen.Count);

                return room;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room details for room {RoomId}", maPhong);
                return null;
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
             lp.SoLuongChoNgoi,
            COUNT(p.MaPhong) as SoPhongKhaDung
     FROM LoaiPhong lp
LEFT JOIN Phong p ON lp.MaLoaiPhong = p.MaLoaiPhong AND p.TinhTrang = N'Hoạt động'
    GROUP BY lp.MaLoaiPhong, lp.TenLoaiPhong, lp.SoLuongChoNgoi
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
        /// Bổ sung thông tin cơ bản cho phòng (chỉ vị trí)
        /// </summary>
        private async Task EnhanceBasicRoomInfoAsync(AvailableRoomDto room)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

                var roomDetailSql = @"
     SELECT p.ViTri 
     FROM Phong p 
 WHERE p.MaPhong = @maPhong";

                var roomDetail = await conn.QueryFirstOrDefaultAsync<dynamic>(roomDetailSql, new { maPhong = room.MaPhong });
                if (roomDetail != null)
                {
                    room.ViTri = roomDetail.ViTri;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enhancing basic room info for room {RoomId}", room.MaPhong);
            }
        }
    }

    /// <summary>
    /// DTO để map kết quả từ stored procedure sp_TimPhongTrong_Web
    /// </summary>
    internal class AvailableRoomFromSPDto
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public string SoLuongChoNgoi { get; set; } = string.Empty;
        public string TrangThaiHienThi { get; set; } = string.Empty;
        public string GioBatDau { get; set; } = string.Empty;
        public string GioKetThuc { get; set; } = string.Empty;
    }
}