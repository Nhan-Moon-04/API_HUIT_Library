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

        public async Task<RoomCapacityLimitsDto?> GetRoomCapacityLimitsAsync(int maLoaiPhong)
        {
            try
            {
                // Lấy thông tin loại phòng từ database
                var loaiPhong = await _context.LoaiPhongs
                    .Where(lp => lp.MaLoaiPhong == maLoaiPhong)
                    .Select(lp => new { lp.MaLoaiPhong, lp.TenLoaiPhong, lp.SoLuongChoNgoi })
                    .FirstOrDefaultAsync();

                if (loaiPhong == null)
                    return null;

                // ✅ Áp dụng logic giống như trigger để xác định min/max
                var result = new RoomCapacityLimitsDto
                {
                    MaLoaiPhong = loaiPhong.MaLoaiPhong,
                    TenLoaiPhong = loaiPhong.TenLoaiPhong,
                    SucChuaToiDa = loaiPhong.SoLuongChoNgoi
                };

                // ✅ Logic giống trigger database
                switch (maLoaiPhong)
                {
                    case 1: // Phòng học nhóm
                        result.SoLuongToiThieu = 5;
                        result.SoLuongToiDa = 7;
                        result.MoTa = "Phòng học nhóm chỉ cho phép từ 5 đến 7 người tham gia.";
                        break;

                    case 2: // Phòng hội thảo
                        result.SoLuongToiThieu = 50;
                        result.SoLuongToiDa = 90;
                        result.MoTa = "Phòng hội thảo chỉ cho phép từ 50 đến 90 người tham gia.";
                        break;

                    case 3: // Phòng thuyết trình
                        result.SoLuongToiThieu = 8;
                        result.SoLuongToiDa = 20;
                        result.MoTa = "Phòng thuyết trình chỉ cho phép từ 8 đến 20 người tham gia.";
                        break;

                    case 4: // Phòng nghiên cứu
                        result.SoLuongToiThieu = 1;
                        result.SoLuongToiDa = 15;
                        result.MoTa = "Phòng nghiên cứu chỉ cho phép tối đa 15 người tham gia.";
                        break;

                    default:
                        // Fallback cho các loại phòng khác
                        result.SoLuongToiThieu = 1;
                        result.SoLuongToiDa = loaiPhong.SoLuongChoNgoi;
                        result.MoTa = $"Cho phép từ 1 đến {loaiPhong.SoLuongChoNgoi} người tham gia.";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log error nếu có logger
                // _logger?.LogError(ex, "Error getting capacity limits for room type {MaLoaiPhong}", maLoaiPhong);
                return null;
            }
        }

        public async Task<IEnumerable<RoomCapacityLimitsDto>> GetAllRoomCapacityLimitsAsync()
        {
            try
            {
                // Lấy tất cả loại phòng từ database
                var allLoaiPhong = await _context.LoaiPhongs
                    .Select(lp => new { lp.MaLoaiPhong, lp.TenLoaiPhong, lp.SoLuongChoNgoi })
                    .ToListAsync();

                var results = new List<RoomCapacityLimitsDto>();

                foreach (var loaiPhong in allLoaiPhong)
                {
                    var result = new RoomCapacityLimitsDto
                    {
                        MaLoaiPhong = loaiPhong.MaLoaiPhong,
                        TenLoaiPhong = loaiPhong.TenLoaiPhong,
                        SucChuaToiDa = loaiPhong.SoLuongChoNgoi
                    };

                    // ✅ Logic giống trigger database
                    switch (loaiPhong.MaLoaiPhong)
                    {
                        case 1: // Phòng học nhóm
                            result.SoLuongToiThieu = 5;
                            result.SoLuongToiDa = 7;
                            result.MoTa = "Phòng học nhóm chỉ cho phép từ 5 đến 7 người tham gia.";
                            break;

                        case 2: // Phòng hội thảo
                            result.SoLuongToiThieu = 50;
                            result.SoLuongToiDa = 90;
                            result.MoTa = "Phòng hội thảo chỉ cho phép từ 50 đến 90 người tham gia.";
                            break;

                        case 3: // Phòng thuyết trình
                            result.SoLuongToiThieu = 8;
                            result.SoLuongToiDa = 20;
                            result.MoTa = "Phòng thuyết trình chỉ cho phép từ 8 đến 20 người tham gia.";
                            break;

                        case 4: // Phòng nghiên cứu
                            result.SoLuongToiThieu = 1;
                            result.SoLuongToiDa = 15;
                            result.MoTa = "Phòng nghiên cứu chỉ cho phép tối đa 15 người tham gia.";
                            break;

                        default:
                            // Fallback cho các loại phòng khác
                            result.SoLuongToiThieu = 1;
                            result.SoLuongToiDa = loaiPhong.SoLuongChoNgoi;
                            result.MoTa = $"Cho phép từ 1 đến {loaiPhong.SoLuongChoNgoi} người tham gia.";
                            break;
                    }

                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                // Log error nếu có logger
                // _logger?.LogError(ex, "Error getting all capacity limits");
                return new List<RoomCapacityLimitsDto>();
            }
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

        public async Task<RoomStatusDto> GetCurrentRoomStatusAsync()
        {
            try
            {
                var nowVn = GetVietnamTime(); // Giờ Việt Nam hiện tại

                // 1. Lấy tổng số phòng theo loại (bỏ filter TinhTrang vì có thể null)
                var allRooms = await _context.Phongs
                    .Include(p => p.MaLoaiPhongNavigation)
                    .ToListAsync();

                // 2. Lấy các phòng đang bận (có booking đang sử dụng)
                var busyRooms = await (from dk in _context.DangKyPhongs
                                       where dk.MaTrangThai == 4 && // Đang sử dụng
                                       dk.ThoiGianBatDau <= nowVn &&
                                       dk.ThoiGianKetThuc >= nowVn &&
                                       dk.MaPhong.HasValue
                                       select dk.MaPhong.Value).ToListAsync();

                // 3. Tính toán thống kê tổng
                var totalRooms = allRooms.Count;
                var busyRoomsCount = busyRooms.Count;
                var availableRoomsCount = totalRooms - busyRoomsCount;

                // 4. Tính toán theo loại phòng
                var roomTypeStats = allRooms
                    .GroupBy(r => new { r.MaLoaiPhong, r.MaLoaiPhongNavigation?.TenLoaiPhong })
                    .Select(g =>
                    {
                        var totalByType = g.Count();
                        var busyByType = g.Count(r => busyRooms.Contains(r.MaPhong));
                        var availableByType = totalByType - busyByType;

                        return new RoomTypeStatusDto
                        {
                            MaLoaiPhong = g.Key.MaLoaiPhong,
                            TenLoaiPhong = g.Key.TenLoaiPhong ?? "Không xác định",
                            TongSo = totalByType,
                            SoPhongBan = busyByType,
                            SoPhongTrong = availableByType,
                            PhanTramTrong = totalByType > 0 ? Math.Round((double)availableByType / totalByType * 100, 1) : 0
                        };
                    }).ToList();

                // 5. Tạo kết quả
                var result = new RoomStatusDto
                {
                    TongSoPhong = totalRooms,
                    SoPhongTrong = availableRoomsCount,
                    SoPhongBan = busyRoomsCount,
                    PhanTramPhongTrong = totalRooms > 0 ? Math.Round((double)availableRoomsCount / totalRooms * 100, 1) : 0,
                    PhanTramPhongBan = totalRooms > 0 ? Math.Round((double)busyRoomsCount / totalRooms * 100, 1) : 0,
                    ThoiGianKiemTra = nowVn,
                    ChiTietTheoLoaiPhong = roomTypeStats
                };

                return result;
            }
            catch (Exception ex)
            {
                // Log error nếu có logger
                // _logger?.LogError(ex, "Error getting current room status");

                // Trả về kết quả mặc định khi có lỗi
                return new RoomStatusDto
                {
                    TongSoPhong = 0,
                    SoPhongTrong = 0,
                    SoPhongBan = 0,
                    PhanTramPhongTrong = 0,
                    PhanTramPhongBan = 0,
                    ThoiGianKiemTra = DateTime.Now,
                    ChiTietTheoLoaiPhong = new List<RoomTypeStatusDto>()
                };
            }
        }

        // Helper method để lấy giờ Việt Nam
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }
    }
}
