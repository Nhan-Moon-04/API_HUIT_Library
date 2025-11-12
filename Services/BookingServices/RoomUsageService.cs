using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HUIT_Library.Services.BookingServices
{
    public class RoomUsageService : IRoomUsageService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<RoomUsageService> _logger;

        // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public RoomUsageService(HuitThuVienContext context, ILogger<RoomUsageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string? Message)> StartRoomUsageAsync(int userId, int maDangKy)
        {
            try
            {
                var booking = await _context.DangKyPhongs.FindAsync(maDangKy);
                if (booking == null)
                    return (false, "Đăng ký không tồn tại.");

                if (booking.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền bắt đầu sử dụng đăng ký này.");

                // Chỉ cho phép check-in khi đã được duyệt (trạng thái 2)
                if (booking.MaTrangThai != 2)
                {
                    var statusMessage = booking.MaTrangThai switch
                    {
                        1 => "Đăng ký đang chờ duyệt, chưa thể check-in",
                        3 => "Đăng ký đã bị từ chối",
                        4 => "Đã check-in rồi",
                        5 => "Đăng ký đã bị hủy",
                        7 => "Đăng ký đã hoàn thành",
                        _ => "Trạng thái không hợp lệ để check-in"
                    };
                    return (false, statusMessage);
                }

                var now = GetVietnamTime();

                // Kiểm tra thời gian check-in (cho phép từ 15 phút trước đến 5 phút sau giờ bắt đầu)
                var minutesUntilStart = (booking.ThoiGianBatDau - now).TotalMinutes;
                if (minutesUntilStart > 15)
                    return (false, $"Chỉ có thể check-in từ 15 phút trước giờ bắt đầu. Còn {minutesUntilStart:F0} phút.");

                if (minutesUntilStart < -5)
                    return (false, "Đã quá thời gian check-in (5 phút sau giờ bắt đầu).");

                // Kiểm tra xem đã có bản ghi sử dụng chưa
                var existingUsage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
                if (existingUsage != null)
                    return (false, "Đã check-in rồi.");

                // Tạo bản ghi sử dụng phòng
                var usage = new SuDungPhong
                {
                    MaDangKy = maDangKy,
                    GioBatDauThucTe = now,
                    TinhTrangPhong = "Tốt", // Mặc định ban đầu
                    GhiChu = "Check-in bởi người dùng"
                };

                _context.SuDungPhongs.Add(usage);

                // Cập nhật trạng thái đăng ký thành "đang sử dụng" (4)
                booking.MaTrangThai = 4;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully started room usage for user {UserId}, booking {MaDangKy}", userId, maDangKy);

                return (true, "Check-in thành công! Bạn có thể bắt đầu sử dụng phòng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting room usage for user {UserId}, booking {MaDangKy}", userId, maDangKy);
                return (false, "Lỗi hệ thống khi check-in. Vui lòng thử lại.");
            }
        }

        public async Task<RoomUsageStatusDto?> GetRoomUsageStatusAsync(int userId, int maDangKy)
        {
            try
            {
                _logger.LogInformation("Getting room usage status for user {UserId}, booking {MaDangKy}", userId, maDangKy);

                var query = from dk in _context.DangKyPhongs
                            join p2 in _context.Phongs on dk.MaPhong equals p2.MaPhong into phongGroup2
                            from roomInfo in phongGroup2.DefaultIfEmpty()
                            join sd in _context.SuDungPhongs on dk.MaDangKy equals sd.MaDangKy into sdGroup
                            from usageRecord in sdGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && dk.MaDangKy == maDangKy
                            select new { dk, roomInfo, usageRecord };

                var result = await query.FirstOrDefaultAsync();
                if (result == null)
                    return null;

                var booking = result.dk;
                var phong = result.roomInfo;
                var usage = result.usageRecord;

                var now = GetVietnamTime();
                var minutesRemaining = (int)(booking.ThoiGianKetThuc - now).TotalMinutes;

                var canExtend = booking.MaTrangThai == 4 && // đang sử dụng
              minutesRemaining > 15 &&
                       now >= booking.ThoiGianBatDau &&
            now <= booking.ThoiGianKetThuc;

                var canComplete = booking.MaTrangThai == 4; // đang sử dụng

                string statusDescription = booking.MaTrangThai switch
                {
                    1 => "Đang chờ duyệt",
                    2 => "Đã duyệt - Chưa check-in",
                    3 => "Đã bị từ chối",
                    4 when minutesRemaining > 0 => $"Đang sử dụng - Còn {minutesRemaining} phút",
                    4 when minutesRemaining <= 0 => "Đang sử dụng - Đã quá giờ, cần trả phòng",
                    5 => "Đã hủy",
                    7 => "Đã hoàn thành",
                    _ => "Trạng thái không xác định"
                };

                return new RoomUsageStatusDto
                {
                    MaDangKy = booking.MaDangKy,
                    MaPhong = booking.MaPhong ?? 0,
                    TenPhong = phong?.TenPhong ?? "Chưa xác định",
                    GioBatDauThucTe = usage?.GioBatDauThucTe,
                    ThoiGianKetThucDuKien = booking.ThoiGianKetThuc,
                    MinutesRemaining = Math.Max(0, minutesRemaining),
                    TinhTrangPhong = usage?.TinhTrangPhong,
                    CanExtend = canExtend,
                    CanComplete = canComplete,
                    StatusDescription = statusDescription
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room usage status for user {UserId}, booking {MaDangKy}", userId, maDangKy);
                return null;
            }
        }

        public async Task<(bool Success, string? Message)> UpdateRoomConditionAsync(int userId, int maDangKy, string tinhTrangPhong, string? ghiChu = null)
        {
            try
            {
                var booking = await _context.DangKyPhongs.FindAsync(maDangKy);
                if (booking == null)
                    return (false, "Đăng ký không tồn tại.");

                if (booking.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền cập nhật tình trạng phòng này.");

                var usage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
                if (usage == null)
                    return (false, "Chưa có bản ghi sử dụng phòng.");

                // Cập nhật tình trạng phòng
                usage.TinhTrangPhong = tinhTrangPhong;

                if (!string.IsNullOrEmpty(ghiChu))
                {
                    if (string.IsNullOrEmpty(usage.GhiChu))
                        usage.GhiChu = ghiChu;
                    else
                        usage.GhiChu += $" - {ghiChu}";
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated room condition for user {UserId}, booking {MaDangKy}, condition: {TinhTrangPhong}",
             userId, maDangKy, tinhTrangPhong);

                return (true, "Cập nhật tình trạng phòng thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room condition for user {UserId}, booking {MaDangKy}", userId, maDangKy);
                return (false, "Lỗi hệ thống khi cập nhật tình trạng phòng. Vui lòng thử lại.");
            }
        }

        public async Task<List<RoomUsageHistoryDto>> GetRoomUsageHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting room usage history for user {UserId}, page {PageNumber}, size {PageSize}",
                          userId, pageNumber, pageSize);

                var query = from dk in _context.DangKyPhongs
                            join sd in _context.SuDungPhongs on dk.MaDangKy equals sd.MaDangKy
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join viPham in _context.ViPhams on sd.MaSuDung equals viPham.MaSuDung into viPhamGroup
                            from v in viPhamGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && sd.GioBatDauThucTe != null
                            orderby sd.GioBatDauThucTe descending
                            select new { dk, sd, p, loaiPhong, hasViolation = v != null };

                var results = await query
                    .Skip((pageNumber - 1) * pageSize)
                     .Take(pageSize)
                          .ToListAsync();

                var roomUsageHistory = results.Select(item =>
      {
          var dk = item.dk;
          var sd = item.sd;
          var p = item.p;
          var loaiPhong = item.loaiPhong;

          TimeSpan? actualUsageTime = null;
          if (sd.GioBatDauThucTe != null && sd.GioKetThucThucTe != null)
          {
              actualUsageTime = sd.GioKetThucThucTe - sd.GioBatDauThucTe;
          }

          return new RoomUsageHistoryDto
          {
              MaDangKy = dk.MaDangKy,
              MaPhong = dk.MaPhong ?? 0,
              TenPhong = p?.TenPhong ?? "Chưa xác định",
              TenLoaiPhong = loaiPhong?.TenLoaiPhong,
              ThoiGianDangKy = dk.ThoiGianBatDau,
              ThoiGianKetThucDangKy = dk.ThoiGianKetThuc,
              GioBatDauThucTe = sd.GioBatDauThucTe,
              GioKetThucThucTe = sd.GioKetThucThucTe,
              ThoiGianSuDungThucTe = actualUsageTime,
              TinhTrangPhong = sd.TinhTrangPhong,
              GhiChu = sd.GhiChu,
              HasViolation = item.hasViolation
          };
      }).ToList();

                _logger.LogInformation("Found {Count} room usage history records for user {UserId}", roomUsageHistory.Count, userId);
                return roomUsageHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room usage history for user {UserId}", userId);
                return new List<RoomUsageHistoryDto>();
            }
        }
    }
}