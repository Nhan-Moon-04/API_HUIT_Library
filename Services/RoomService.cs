using Dapper;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace HUIT_Library.Services
{
    public class RoomService : IRoomService
    {
        private readonly HuitThuVienContext _context;

        public RoomService(HuitThuVienContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomDto>> SearchRoomsAsync(SearchRoomRequest request)
        {
            // Basic search: filter by keyword, capacity, and availability window (using LichTrangThaiPhong)
            using var conn = _context.Database.GetDbConnection();
            if (conn.State == System.Data.ConnectionState.Closed)
                await conn.OpenAsync();

            var sql = @"SELECT DISTINCT p.MaPhong, p.TenPhong, lp.TenLoaiPhong, lp.SoLuongChoNgoi AS SucChua, ltp.TrangThai
FROM Phong p
JOIN LoaiPhong lp ON p.MaLoaiPhong = lp.MaLoaiPhong
LEFT JOIN LichTrangThaiPhong ltp ON p.MaPhong = ltp.MaPhong";

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                filters.Add("(p.TenPhong LIKE @kw OR lp.TenLoaiPhong LIKE @kw)");
            }
            if (request.MinimumCapacity.HasValue)
            {
                // SoLuongChoNgoi is a string in the model; use TRY_CAST to avoid errors if non-numeric
                filters.Add("TRY_CAST(lp.SoLuongChoNgoi AS INT) >= @minCap");
            }
            if (request.Date.HasValue)
            {
                filters.Add("ltp.Ngay = @date");
                // If client provided both start and end time parse them into valid SQL time strings
                if (!string.IsNullOrWhiteSpace(request.StartTime) && !string.IsNullOrWhiteSpace(request.EndTime))
                {
                    // assume client sends HH:mm or HH:mm:ss or ISO time; compare to TimeOnly fields
                    filters.Add("NOT (@startTime < ltp.GioKetThuc AND @endTime > ltp.GioBatDau)");
                }
            }

            if (filters.Any())
            {
                sql += " WHERE " + string.Join(" AND ", filters);
            }

            // Prepare parameters; Dapper will map nulls appropriately
            object param = new
            {
                kw = "%" + (request.Keyword ?? string.Empty) + "%",
                minCap = request.MinimumCapacity,
                date = request.Date?.ToString("yyyy-MM-dd"),
                startTime = ParseTimeToSql(request.StartTime),
                endTime = ParseTimeToSql(request.EndTime)
            };

            var results = await conn.QueryAsync<RoomDto>(sql, param);

            return results;
        }

        public async Task<RoomDetailsDto?> GetRoomDetailsAsync(int roomId)
        {
            using var conn = _context.Database.GetDbConnection();
            if (conn.State == System.Data.ConnectionState.Closed)
                await conn.OpenAsync();

            var roomSql = @"SELECT p.MaPhong, p.TenPhong, lp.TenLoaiPhong, lp.SoLuongChoNgoi as SucChua, p.MaTrangThai as TinhTrang
FROM Phong p
JOIN LoaiPhong lp ON p.MaLoaiPhong = lp.MaLoaiPhong
WHERE p.MaPhong = @id";

            var room = await conn.QueryFirstOrDefaultAsync<RoomDetailsDto>(roomSql, new { id = roomId });
            if (room == null) return null;

            var resourcesSql = @"SELECT pn.MaTaiNguyen as MaTaiNguyen, tn.TenTaiNguyen as TenTaiNguyen, pn.SoLuong as SoLuong, pn.TinhTrang as TinhTrang
FROM PhongTaiNguyen pn
JOIN TaiNguyen tn ON pn.MaTaiNguyen = tn.MaTaiNguyen
WHERE pn.MaPhong = @id";

            var resources = await conn.QueryAsync<ResourceDto>(resourcesSql, new { id = roomId });
            room.TaiNguyen = resources;

            var scheduleSql = @"SELECT MaLich, MaPhong, Ngay, GioBatDau, GioKetThuc, TrangThai, GhiChu
FROM LichTrangThaiPhong
WHERE MaPhong = @id
ORDER BY Ngay DESC, GioBatDau DESC";

            var schedules = await conn.QueryAsync<RoomScheduleDto>(scheduleSql, new { id = roomId });
            room.LichSu = schedules;

            return room;
        }

        // Helper to normalize time strings into SQL-compatible format (HH:mm:ss) or null
        private static string? ParseTimeToSql(string? time)
        {
            if (string.IsNullOrWhiteSpace(time)) return null;
            // Try parse as TimeOnly or DateTime
            if (TimeOnly.TryParse(time, out var t))
                return t.ToString("HH:mm:ss");
            if (DateTime.TryParse(time, out var dt))
                return dt.ToString("HH:mm:ss");
            return null;
        }
    }
}
