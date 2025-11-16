using HUIT_Library.DTOs.Response;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HUIT_Library.Services.BookingServices
{
    public class BookingViewService : IBookingViewService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<BookingViewService> _logger;

        // DB status constants
        private const int DB_PENDING = 1;
        private const int DB_APPROVED = 2;
        private const int DB_REJECTED = 3;
        private const int DB_INUSE = 4;
        private const int DB_CANCELLED = 5;
        private const int DB_USED = 7;

        // Logical booking statuses used throughout the app
        private enum BookingStatus
        {
            Pending = 1, // Chờ duyệt
            Approved = 2, // Đã duyệt
            InUse = 3, // Đang sử dụng
            Rejected = 4, // Từ chối
            Cancelled = 5, // Hủy
            Used = 6 // Đã sử dụng
        }

        // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        // Map DB values to normalized BookingStatus
        private BookingStatus MapDbToStatus(int? dbValue)
        {
            return dbValue switch
            {
                DB_PENDING => BookingStatus.Pending,
                DB_APPROVED => BookingStatus.Approved,
                DB_INUSE => BookingStatus.InUse,
                DB_REJECTED => BookingStatus.Rejected,
                DB_CANCELLED => BookingStatus.Cancelled,
                DB_USED => BookingStatus.Used,
                _ => BookingStatus.Pending
            };
        }

        private string GetStatusName(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => "Chờ duyệt",
                BookingStatus.Approved => "Đã duyệt",
                BookingStatus.InUse => "Đang sử dụng",
                BookingStatus.Rejected => "Từ chối",
                BookingStatus.Cancelled => "Hủy",
                BookingStatus.Used => "Đã sử dụng",
                _ => "Không xác định"
            };
        }

        public BookingViewService(HuitThuVienContext context, ILogger<BookingViewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<BookingHistoryDto>> GetBookingHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting booking history for user {UserId}, page {PageNumber}, size {PageSize}",
               userId, pageNumber, pageSize);

                // Lấy lịch sử đặt phòng của user (bao gồm đã từ chối(3), huỷ(5), đã sử dụng(7))
                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            join suDung in _context.SuDungPhongs on dk.MaDangKy equals suDung.MaDangKy into suDungGroup
                            from sd in suDungGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && (dk.MaTrangThai == DB_REJECTED || dk.MaTrangThai == DB_CANCELLED || dk.MaTrangThai == DB_USED)
                            orderby dk.ThoiGianBatDau descending
                            select new BookingHistoryDto
                            {
                                MaDangKy = dk.MaDangKy,
                                MaPhong = dk.MaPhong ?? 0,
                                TenPhong = p != null ? p.TenPhong : "Chưa phân phòng",
                                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                                ThoiGianBatDau = dk.ThoiGianBatDau,
                                ThoiGianKetThuc = dk.ThoiGianKetThuc,
                                GioBatDauThucTe = sd != null ? sd.GioBatDauThucTe : null,
                                GioKetThucThucTe = sd != null ? sd.GioKetThucThucTe : null,
                                LyDo = dk.LyDo,
                                SoLuong = dk.SoLuong,
                                GhiChu = dk.GhiChu,
                                MaTrangThai = dk.MaTrangThai ?? 0,
                                TenTrangThai = tt != null ? tt.TenTrangThai : "Không xác định",
                                NgayDuyet = dk.NgayDuyet,
                                NgayMuon = dk.NgayMuon,
                                TinhTrangPhong = sd != null ? sd.TinhTrangPhong : null,
                                GhiChuSuDung = sd != null ? sd.GhiChu : null
                            };

                // Phân trang
                var result = await query
                .Skip((pageNumber - 1) * pageSize)
                          .Take(pageSize)
                         .ToListAsync();

                // ✅ Thêm thông tin vi phạm
                await EnhanceBookingHistoryWithViolationsAsync(result);

                _logger.LogInformation("Found {Count} booking history records for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking history for user {UserId}", userId);
                return new List<BookingHistoryDto>();
            }
        }

        public async Task<List<CurrentBookingDto>> GetCurrentBookingsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting current bookings for user {UserId}", userId);

                var nowVn = GetVietnamTime();

                // Lấy các đăng ký hiện tại:
                // - luôn lấy các bản ghi đang sử dụng (DB_INUSE)
                // - lấy các bản ghi chờ duyệt (DB_PENDING) hoặc đã duyệt (DB_APPROVED)
                // chỉ khi chưa quá thời gian kết thúc
                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && (
                    dk.MaTrangThai == DB_INUSE ||
                            ((dk.MaTrangThai == DB_PENDING || dk.MaTrangThai == DB_APPROVED) &&
                       dk.ThoiGianKetThuc >= nowVn)
                           )
                            orderby dk.ThoiGianBatDau
                            select new { dk, p, loaiPhong, tt };

                var bookings = await query.ToListAsync();

                var result = bookings.Select(item =>
                   {
                       var dk = item.dk;
                       var p = item.p;
                       var loaiPhong = item.loaiPhong;
                       var tt = item.tt;

                       var normalizedStatus = MapDbToStatus(dk.MaTrangThai);

                       var start = dk.ThoiGianBatDau;
                       var end = dk.ThoiGianKetThuc;

                       // Tính toán thời gian
                       var minutesUntilStart = (int)(start - nowVn).TotalMinutes;
                       var minutesRemaining = (int)(end - nowVn).TotalMinutes;

                       // Actions
                       var canStart = normalizedStatus == BookingStatus.Approved &&
          minutesUntilStart <= 15 && minutesUntilStart >= -5;

                       var canExtend = normalizedStatus == BookingStatus.InUse &&
     minutesRemaining > 15 && nowVn >= start && nowVn <= end;

                       var canComplete = normalizedStatus == BookingStatus.InUse;

                       // Status description
                       string statusDescription = normalizedStatus switch
                       {
                           BookingStatus.Pending => minutesUntilStart > 0 ? $"Chờ duyệt - Bắt đầu sau {minutesUntilStart} phút" : "Chờ duyệt - Đã đến giờ",
                           BookingStatus.Approved when minutesUntilStart > 15 => $"Đã duyệt - Có thể checkin sau {minutesUntilStart - 15} phút",
                           BookingStatus.Approved when minutesUntilStart <= 15 && minutesUntilStart > 0 => "Đã duyệt - Có thể checkin ngay",
                           BookingStatus.Approved when minutesUntilStart <= 0 && minutesRemaining > 0 => "Đã duyệt - Đã đến giờ, có thể checkin",
                           BookingStatus.Approved when minutesRemaining <= 0 => "Đã duyệt - Đã quá giờ",
                           BookingStatus.InUse when minutesRemaining > 0 => $"Đang sử dụng - Còn {minutesRemaining} phút",
                           BookingStatus.InUse when minutesRemaining <= 0 => "Đang sử dụng - Đã quá giờ, cần trả phòng",
                           _ => GetStatusName(normalizedStatus)
                       };

                       return new CurrentBookingDto
                       {
                           MaDangKy = dk.MaDangKy,
                           MaPhong = dk.MaPhong,
                           TenPhong = p?.TenPhong ?? "Chưa phân phòng",
                           TenLoaiPhong = loaiPhong?.TenLoaiPhong,
                           ThoiGianBatDau = start,
                           ThoiGianKetThuc = end,
                           LyDo = dk.LyDo,
                           SoLuong = dk.SoLuong,
                           GhiChu = dk.GhiChu,
                           MaTrangThai = dk.MaTrangThai ?? 0,
                           TenTrangThai = GetStatusName(normalizedStatus),
                           NgayDuyet = dk.NgayDuyet,
                           NgayMuon = dk.NgayMuon,

                           CanStart = canStart,
                           CanExtend = canExtend,
                           CanComplete = canComplete,
                           StatusDescription = statusDescription,
                           MinutesUntilStart = Math.Max(0, minutesUntilStart),
                           MinutesRemaining = Math.Max(0, minutesRemaining)
                       };
                   }).ToList();

                // ✅ Thêm thông tin vi phạm
                await EnhanceCurrentBookingWithViolationsAsync(result);

                _logger.LogInformation("Found {Count} current bookings for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current bookings for user {UserId}", userId);
                return new List<CurrentBookingDto>();
            }
        }

        public async Task<BookingHistoryDto?> GetBookingDetailsAsync(int userId, int maDangKy)
        {
            try
            {
                _logger.LogInformation("Getting booking details for user {UserId}, booking {MaDangKy}", userId, maDangKy);

                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            join suDung in _context.SuDungPhongs on dk.MaDangKy equals suDung.MaDangKy into suDungGroup
                            from sd in suDungGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && dk.MaDangKy == maDangKy
                            select new BookingHistoryDto
                            {
                                MaDangKy = dk.MaDangKy,
                                MaPhong = dk.MaPhong ?? 0,
                                TenPhong = p != null ? p.TenPhong : "Chưa phân phòng",
                                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                                ThoiGianBatDau = dk.ThoiGianBatDau,
                                ThoiGianKetThuc = dk.ThoiGianKetThuc,
                                GioBatDauThucTe = sd != null ? sd.GioBatDauThucTe : null,
                                GioKetThucThucTe = sd != null ? sd.GioKetThucThucTe : null,
                                LyDo = dk.LyDo,
                                SoLuong = dk.SoLuong,
                                GhiChu = dk.GhiChu,
                                MaTrangThai = dk.MaTrangThai ?? 0,
                                TenTrangThai = tt != null ? tt.TenTrangThai : "Không xác định",
                                NgayDuyet = dk.NgayDuyet,
                                NgayMuon = dk.NgayMuon,
                                TinhTrangPhong = sd != null ? sd.TinhTrangPhong : null,
                                GhiChuSuDung = sd != null ? sd.GhiChu : null
                            };

                var result = await query.FirstOrDefaultAsync();

                if (result != null)
                {
                    _logger.LogInformation("Found booking details for user {UserId}, booking {MaDangKy}", userId, maDangKy);
                }
                else
                {
                    _logger.LogWarning("Booking {MaDangKy} not found for user {UserId}", maDangKy, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking details for user {UserId}, booking {MaDangKy}", userId, maDangKy);
                return null;
            }
        }

        public async Task<List<BookingHistoryDto>> SearchBookingHistoryAsync(int userId, string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Searching booking history for user {UserId}, term '{SearchTerm}', page {PageNumber}",
                  userId, searchTerm, pageNumber);

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetBookingHistoryAsync(userId, pageNumber, pageSize);
                }

                var lowerSearchTerm = searchTerm.ToLower();

                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            join suDung in _context.SuDungPhongs on dk.MaDangKy equals suDung.MaDangKy into suDungGroup
                            from sd in suDungGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId &&
                           // ✅ Sửa lại: chỉ tìm trong lịch sử (đã thuê, trả, hủy)
                           (dk.MaTrangThai == DB_REJECTED || dk.MaTrangThai == DB_CANCELLED || dk.MaTrangThai == DB_USED) &&
                        (
                               (p != null && p.TenPhong.ToLower().Contains(lowerSearchTerm)) ||
                             (loaiPhong.TenLoaiPhong != null && loaiPhong.TenLoaiPhong.ToLower().Contains(lowerSearchTerm)) ||
                          (dk.LyDo != null && dk.LyDo.ToLower().Contains(lowerSearchTerm)) ||
                      (tt != null && tt.TenTrangThai.ToLower().Contains(lowerSearchTerm)) ||
                          dk.MaDangKy.ToString().Contains(searchTerm) ||
 (dk.GhiChu != null && dk.GhiChu.ToLower().Contains(lowerSearchTerm))
                              )
                            orderby dk.ThoiGianBatDau descending
                            select new BookingHistoryDto
                            {
                                MaDangKy = dk.MaDangKy,
                                MaPhong = dk.MaPhong ?? 0,
                                TenPhong = p != null ? p.TenPhong : "Chưa phân phòng",
                                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                                ThoiGianBatDau = dk.ThoiGianBatDau,
                                ThoiGianKetThuc = dk.ThoiGianKetThuc,
                                GioBatDauThucTe = sd != null ? sd.GioBatDauThucTe : null,
                                GioKetThucThucTe = sd != null ? sd.GioKetThucThucTe : null,
                                LyDo = dk.LyDo,
                                SoLuong = dk.SoLuong,
                                GhiChu = dk.GhiChu,
                                MaTrangThai = dk.MaTrangThai ?? 0,
                                TenTrangThai = tt != null ? tt.TenTrangThai : "Không xác định",
                                NgayDuyet = dk.NgayDuyet,
                                NgayMuon = dk.NgayMuon,
                                TinhTrangPhong = sd != null ? sd.TinhTrangPhong : null,
                                GhiChuSuDung = sd != null ? sd.GhiChu : null
                            };

                var result = await query
                      .Skip((pageNumber - 1) * pageSize)
                      .Take(pageSize)
             .ToListAsync();

                // ✅ Thêm thông tin vi phạm
                await EnhanceBookingHistoryWithViolationsAsync(result);

                _logger.LogInformation("Found {Count} booking records for search term '{SearchTerm}', user {UserId}",
                     result.Count, searchTerm, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching booking history for user {UserId}, term '{SearchTerm}'", userId, searchTerm);
                return new List<BookingHistoryDto>();
            }
        }

        // ✅ Helper methods để thêm thông tin vi phạm
        private async Task EnhanceBookingHistoryWithViolationsAsync(List<BookingHistoryDto> bookings)
        {
            try
            {
                var bookingIds = bookings.Select(b => b.MaDangKy).ToList();

                // Lấy tất cả vi phạm cho các booking này
                var violations = await (from v in _context.ViPhams
                                        join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                                        join qd in _context.QuyDinhViPhams on v.MaQuyDinh equals qd.MaQuyDinh into qdGroup
                                        from quyDinh in qdGroup.DefaultIfEmpty()
                                        where bookingIds.Contains(sd.MaDangKy)
                                        select new
                                        {
                                            MaDangKy = sd.MaDangKy,
                                            MaViPham = v.MaViPham,
                                            TenViPham = quyDinh != null ? quyDinh.TenViPham : "Không xác định",
                                            NgayLap = v.NgayLap,
                                            TrangThaiXuLy = v.TrangThaiXuLy
                                        }).ToListAsync();

                // Group vi phạm theo booking
                var violationsByBooking = violations.GroupBy(v => v.MaDangKy).ToDictionary(
                 g => g.Key,
                g => g.ToList()
                    );

                // Cập nhật thông tin vi phạm cho từng booking
                foreach (var booking in bookings)
                {
                    if (violationsByBooking.TryGetValue(booking.MaDangKy, out var bookingViolations))
                    {
                        booking.CoBienBan = true;
                        booking.SoLuongBienBan = bookingViolations.Count;
                        booking.DanhSachViPham = bookingViolations.Select(v => new ViolationSummaryDto
                        {
                            MaViPham = v.MaViPham,
                            TenViPham = v.TenViPham,
                            NgayLap = v.NgayLap,
                            TrangThaiXuLy = v.TrangThaiXuLy
                        }).ToList();
                    }
                    else
                    {
                        booking.CoBienBan = false;
                        booking.SoLuongBienBan = 0;
                        booking.DanhSachViPham = new List<ViolationSummaryDto>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enhancing booking history with violations");
                // Đảm bảo không crash API, chỉ log warning
                foreach (var booking in bookings)
                {
                    booking.CoBienBan = false;
                    booking.SoLuongBienBan = 0;
                    booking.DanhSachViPham = new List<ViolationSummaryDto>();
                }
            }
        }

        private async Task EnhanceCurrentBookingWithViolationsAsync(List<CurrentBookingDto> bookings)
        {
            try
            {
                var bookingIds = bookings.Select(b => b.MaDangKy).ToList();

                // Lấy số lượng vi phạm cho các booking này
                var violationCounts = await (from v in _context.ViPhams
                                             join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                                             where bookingIds.Contains(sd.MaDangKy)
                                             group sd by sd.MaDangKy into g
                                             select new
                                             {
                                                 MaDangKy = g.Key,
                                                 Count = g.Count()
                                             }).ToDictionaryAsync(x => x.MaDangKy, x => x.Count);

                // Cập nhật thông tin vi phạm cho từng booking
                foreach (var booking in bookings)
                {
                    if (violationCounts.TryGetValue(booking.MaDangKy, out var violationCount))
                    {
                        booking.CoBienBan = true;
                        booking.SoLuongBienBan = violationCount;
                    }
                    else
                    {
                        booking.CoBienBan = false;
                        booking.SoLuongBienBan = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enhancing current booking with violations");
                // Đảm bảo không crash API, chỉ log warning
                foreach (var booking in bookings)
                {
                    booking.CoBienBan = false;
                    booking.SoLuongBienBan = 0;
                }
            }
        }
    }
}