using Dapper;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Data;

namespace HUIT_Library.Services
{
    public class LibraryStatisticsService : ILibraryStatisticsService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<LibraryStatisticsService> _logger;
        private readonly IMemoryCache _cache;

        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public LibraryStatisticsService(
            HuitThuVienContext context,
            ILogger<LibraryStatisticsService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<LibraryStatisticsDto> GetLibraryStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting library statistics");

                var now = GetVietnamTime();
                var today = now.Date;
                var yesterday = today.AddDays(-1);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                // 1. Tổng lượt truy cập từ VisitLog
                var tongLuotTruyCap = await conn.QuerySingleOrDefaultAsync<long?>(
                    "SELECT COUNT(*) FROM VisitLog") ?? 0L;

                // 2. Thành viên online (có UserId trong VisitLog trong 15 phút gần đây)
                var thanhVienOnline = await conn.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT COUNT(DISTINCT UserId) 
                      FROM VisitLog 
                     WHERE UserId IS NOT NULL 
                       AND VisitTime >= @recentTime",
                    new { recentTime = now.AddMinutes(-15) }) ?? 0;

                // 3. Khách online (IP không có UserId trong 15 phút gần đây)
                var khachOnline = await conn.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT COUNT(DISTINCT IPAddress)
                      FROM VisitLog
                     WHERE UserId IS NULL
                       AND IPAddress IS NOT NULL
                       AND VisitTime >= @recentTime",
                    new { recentTime = now.AddMinutes(-15) }) ?? 0;

                // 4. Lượt truy cập trong ngày từ VisitLog
                var trongNgay = await conn.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT COUNT(*)
                      FROM VisitLog
                     WHERE CAST(VisitTime AS DATE) = @today",
                    new { today }) ?? 0;

                // 5. Lượt truy cập hôm qua từ VisitLog
                var homQua = await conn.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT COUNT(*)
                      FROM VisitLog
                     WHERE CAST(VisitTime AS DATE) = @yesterday",
                    new { yesterday }) ?? 0;

                // 6. Lượt truy cập trong tháng từ VisitLog
                var trongThang = await conn.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT COUNT(*)
                      FROM VisitLog
                     WHERE VisitTime >= @startOfMonth",
                    new { startOfMonth }) ?? 0;

                var statistics = new LibraryStatisticsDto
                {
                    TongLuotTruyCap = tongLuotTruyCap,
                    SoLuongOnline = thanhVienOnline + khachOnline,
                    ThanhVienOnline = thanhVienOnline,
                    KhachOnline = khachOnline,
                    TrongNgay = trongNgay,
                    HomQua = homQua,
                    TrongThang = trongThang
                };

                _logger.LogInformation("Library statistics retrieved successfully: Total={Total}, Online={Online}, Today={Today}, Month={Month}",
                    tongLuotTruyCap, thanhVienOnline + khachOnline, trongNgay, trongThang);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting library statistics");
                
                // Trả về dữ liệu mặc định thay vì empty object
                return new LibraryStatisticsDto
                {
                    TongLuotTruyCap = 0,
                    SoLuongOnline = 0,
                    ThanhVienOnline = 0,
                    KhachOnline = 0,
                    TrongNgay = 0,
                    HomQua = 0,
                    TrongThang = 0
                };
            }
        }

        public async Task RecordVisitAsync(int? userId = null, string? ipAddress = null)
        {
            try
            {
                var now = GetVietnamTime();

                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                // Ghi nhận visit vào VisitLog
                await conn.ExecuteAsync(@"
                    INSERT INTO VisitLog (UserId, IPAddress, VisitTime)
                    VALUES (@userId, @ipAddress, @visitTime)",
                    new { userId, ipAddress, visitTime = now });

                _logger.LogInformation("Visit recorded: UserId={UserId}, IP={IPAddress}, Time={Time}",
                    userId, ipAddress ?? "Unknown", now);

                // Nếu là user đã đăng nhập thì có thể cập nhật LastActivity (nếu có column này)
                if (userId.HasValue)
                {
                    try
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE NguoiDung 
                            SET LastActivity = @lastActivity
                            WHERE MaNguoiDung = @userId",
                            new { userId = userId.Value, lastActivity = now });
                    }
                    catch (Exception updateEx)
                    {
                        // Không có column LastActivity thì bỏ qua
                        _logger.LogDebug(updateEx, "Could not update LastActivity for user {UserId} (column may not exist)", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error recording visit for UserId={UserId}, IP={IPAddress}", userId, ipAddress);
            }
        }

        public async Task UpdateUserOnlineStatusAsync(int userId, bool isOnline)
        {
            try
            {
                var now = GetVietnamTime();

                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                if (isOnline)
                {
                    // Khi user online, ghi nhận vào VisitLog
                    await conn.ExecuteAsync(@"
                        INSERT INTO VisitLog (UserId, IPAddress, VisitTime)
                        VALUES (@userId, @ipAddress, @visitTime)",
                        new { userId, ipAddress = "Online Status Update", visitTime = now });

                    // Cập nhật LastActivity nếu có
                    try
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE NguoiDung 
                            SET LastActivity = @lastActivity
                            WHERE MaNguoiDung = @userId",
                            new { userId, lastActivity = now });
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogDebug(updateEx, "Could not update LastActivity for user {UserId} (column may not exist)", userId);
                    }
                }

                _logger.LogInformation("User {UserId} online status updated: {IsOnline} at {Time}",
                    userId, isOnline, now);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating online status for user {UserId}", userId);
            }
        }
    }
}
