using Dapper;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace HUIT_Library.Services.BookingServices
{
    public class BookingManagementService : IBookingManagementService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<BookingManagementService> _logger;
        private readonly IConfiguration _configuration;

        // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public BookingManagementService(HuitThuVienContext context, IConfiguration configuration, ILogger<BookingManagementService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request)
        {
            try
            {
                // 1️⃣ Kiểm tra dữ liệu đầu vào
                if (request.MaLoaiPhong <= 0)
                    return (false, "Vui lòng chọn loại phòng hợp lệ.");

                if (request.ThoiGianBatDau == default)
                    return (false, "Vui lòng chọn thời gian bắt đầu.");

                // ✅ Kiểm tra số lượng tối thiểu dựa trên loại phòng
                var loaiPhong = await _context.LoaiPhongs.FindAsync(request.MaLoaiPhong);
                if (loaiPhong == null)
                    return (false, "Loại phòng không tồn tại.");

                // Kiểm tra số lượng với sức chứa phòng
                int sucChuaToiDa = loaiPhong.SoLuongChoNgoi;
                int soLuongToiThieu = sucChuaToiDa / 2; // 50% sức chứa

                if (request.SoLuong < soLuongToiThieu)
                {
                    return (false, $"Số lượng người tham gia phải ít nhất {soLuongToiThieu} người (50% sức chứa phòng {sucChuaToiDa} người).");
                }

                if (request.SoLuong > sucChuaToiDa)
                {
                    return (false, $"Số lượng người tham gia không được vượt quá {sucChuaToiDa} người (sức chứa tối đa của phòng).");
                }

                var nowVn = GetVietnamTime();

                // 2️⃣ Kiểm tra vi phạm gần đây
                var cutoff = nowVn.AddMonths(-6);
                var violationCount = await (from v in _context.ViPhams
                                            join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                                            join dk in _context.DangKyPhongs on sd.MaDangKy equals dk.MaDangKy
                                            where dk.MaNguoiDung == userId && v.NgayLap >= cutoff
                                            select v).CountAsync();

                if (violationCount > 3)
                    return (false, $"Bạn có {violationCount} biên bản vi phạm trong 6 tháng gần nhất, nên tạm thời không thể đăng ký.");

                // 3️⃣ Chuẩn bị dữ liệu cho DB
                var startForDb = new DateTime(
              request.ThoiGianBatDau.Year,
                     request.ThoiGianBatDau.Month,
                   request.ThoiGianBatDau.Day,
           request.ThoiGianBatDau.Hour,
              request.ThoiGianBatDau.Minute,
                  request.ThoiGianBatDau.Second,
                DateTimeKind.Unspecified
             );

                var parameters = new DynamicParameters();
                parameters.Add("@MaNguoiDung", userId);
                parameters.Add("@MaLoaiPhong", request.MaLoaiPhong);
                parameters.Add("@ThoiGianBatDau", startForDb);
                parameters.Add("@LyDo", request.LyDo ?? "");
                parameters.Add("@SoLuong", request.SoLuong <= 0 ? 1 : request.SoLuong);
                parameters.Add("@GhiChu", request.GhiChu ?? "");

                // Add output parameters
                parameters.Add("@ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ResultMessage", dbType: DbType.String, size: 255, direction: ParameterDirection.Output);

                // 4️⃣ Gọi stored procedure
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

                await conn.ExecuteAsync("dbo.sp_DangKyPhong", parameters, commandType: CommandType.StoredProcedure);

                // 5️⃣ Lấy kết quả từ output parameters
                var resultCode = parameters.Get<int>("@ResultCode");
                var resultMessage = parameters.Get<string>("@ResultMessage") ?? "Không có thông báo từ hệ thống";

                if (resultCode == 0)
                {
                    // Thành công - tìm bản ghi vừa insert để tạo thông báo
                    var endVn = startForDb.AddHours(2);
                    var inserted = await _context.DangKyPhongs
                  .Where(d => d.MaNguoiDung == userId &&
             d.ThoiGianBatDau == startForDb &&
                      d.ThoiGianKetThuc == endVn)
              .OrderByDescending(d => d.MaDangKy)
                  .FirstOrDefaultAsync();

                    if (inserted != null)
                        await CreateNotificationForBookingAsync(userId, request, inserted.MaDangKy);

                    _logger.LogInformation("Successfully created booking for user {UserId}, booking ID {BookingId}, participants: {SoLuong}",
                userId, inserted?.MaDangKy, request.SoLuong);

                    return (true, resultMessage);
                }
                else
                {
                    // Thất bại - log chi tiết và trả về thông báo thân thiện
                    _logger.LogWarning("Booking creation failed for user {UserId}. Code: {ResultCode}, Message: {ResultMessage}",
                          userId, resultCode, resultMessage);

                    return (false, resultMessage);
                }
            }
            catch (SqlException ex)
            {
                // Log chi tiết cho developer
                _logger.LogError(ex, "SQL error while calling sp_DangKyPhong for user {UserId}. " +
               "Error Number: {ErrorNumber}, Severity: {Severity}, State: {State}",
                 userId, ex.Number, ex.Class, ex.State);

                // Trả về thông báo thân thiện dựa trên loại lỗi SQL
                var userMessage = ex.Number switch
                {
                    2 => "Không thể kết nối đến cơ sở dữ liệu. Vui lòng thử lại sau.",
                    547 => "Dữ liệu không hợp lệ. Vui lòng kiểm tra thông tin đăng ký.",
                    2627 or 2601 => "Bạn đã có đăng ký trùng lặp cho thời gian này.",
                    -2 => "Hệ thống đang quá tải. Vui lòng thử lại sau ít phút.",
                    18456 => "Lỗi xác thực. Vui lòng đăng nhập lại.",
                    _ when ex.Message.Contains("timeout") => "Hệ thống đang bận. Vui lòng thử lại sau.",
                    _ when ex.Message.Contains("deadlock") => "Có xung đột dữ liệu. Vui lòng thử lại.",
                    _ => "Có lỗi xảy ra khi xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ bộ phận hỗ trợ."
                };

                return (false, userMessage);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database connection error while creating booking for user {UserId}", userId);
                return (false, "Không thể kết nối đến hệ thống. Vui lòng thử lại sau.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout error while creating booking for user {UserId}", userId);
                return (false, "Hệ thống đang quá tải. Vui lòng thử lại sau ít phút.");
            }
            catch (Exception ex)
            {
                // Log đầy đủ chi tiết cho developer
                _logger.LogError(ex, "Unexpected error while creating booking for user {UserId}. " +
                      "Request: {@BookingRequest}", userId, new
                      {
                          request.MaLoaiPhong,
                          request.ThoiGianBatDau,
                          request.SoLuong,
                          LyDoLength = request.LyDo?.Length ?? 0,
                          GhiChuLength = request.GhiChu?.Length ?? 0
                      });

                // Trả về thông báo thân thiện cho người dùng
                return (false, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ.");
            }
        }

        private async Task CreateNotificationForBookingAsync(int userId, CreateBookingRequest request, int? maDangKy)
        {
            var title = "Yêu cầu mượn phòng đã được gửi";
            var content = $"Yêu cầu mượn phòng (Loại: {request.MaLoaiPhong}) từ {request.ThoiGianBatDau:dd/MM/yyyy HH:mm} đã được gửi. Vui lòng chờ duyệt.";
            if (maDangKy.HasValue)
            {
                content += $" (Mã đăng ký: {maDangKy.Value})";
            }

            var thongBao = new ThongBao
            {
                MaNguoiDung = userId,
                TieuDe = title,
                NoiDung = content,
                NgayTao = GetVietnamTime(),
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request)
        {
            try
            {
                // Load booking
                var booking = await _context.DangKyPhongs.FindAsync(request.MaDangKy);
                if (booking == null) return (false, "Yêu cầu không tồn tại.");
                if (booking.MaNguoiDung != userId) return (false, "Bạn không có quyền gia hạn yêu cầu này.");

                // Must be currently in use
                var now = GetVietnamTime();
                if (!(booking.ThoiGianBatDau <= now && booking.ThoiGianKetThuc >= now))
                    return (false, "Lượt mượn không đang trong thời gian sử dụng.");

                // Must have more than 15 minutes remaining
                var remaining = booking.ThoiGianKetThuc - now;
                if (remaining.TotalMinutes < 15)
                    return (false, "Không thể gia hạn khi còn dưới 15 phút.");

                // New end time must be extension of original end and within 1-2 hours extension
                if (request.NewEndTime <= booking.ThoiGianKetThuc)
                    return (false, "Thời gian kết thúc mới phải lớn hơn thời gian hiện tại.");

                var extension = request.NewEndTime - booking.ThoiGianKetThuc;
                if (extension <= TimeSpan.Zero || extension > TimeSpan.FromHours(2))
                    return (false, "Gia hạn chỉ cho phép 1-2 giờ.");

                // Check conflicts for the extended interval
                var conflicts = await _context.DangKyPhongs
               .Where(d => d.MaPhong == booking.MaPhong && d.MaDangKy != booking.MaDangKy &&
      !(request.NewEndTime <= d.ThoiGianBatDau || booking.ThoiGianKetThuc >= d.ThoiGianKetThuc))
          .AnyAsync();

                if (conflicts)
                    return (false, "Gia hạn thất bại. Phòng đã được đặt trước.");

                // Auto-approve and update end time
                booking.ThoiGianKetThuc = request.NewEndTime;
                booking.NgayDuyet = GetVietnamTime();
                booking.NguoiDuyet = 0; // system
                booking.MaTrangThai = 2; // approved

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully extended booking {MaDangKy} for user {UserId}", request.MaDangKy, userId);

                return (true, "Gia hạn thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending booking {MaDangKy} for user {UserId}", request.MaDangKy, userId);
                return (false, "Lỗi hệ thống. Vui lòng thử lại.");
            }
        }

        public async Task<(bool Success, string? Message)> CompleteBookingAsync(int userId, int maDangKy)
        {
            try
            {
                // Verify booking exists
                var booking = await _context.DangKyPhongs.FindAsync(maDangKy);
                if (booking == null)
                    return (false, "Đăng ký không tồn tại.");

                if (booking.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền trả phòng cho đăng ký này.");

                // Kiểm tra trạng thái hiện tại - chỉ cho phép trả phòng khi đang sử dụng (trạng thái 4)
                if (booking.MaTrangThai != 4)
                {
                    var statusMessage = booking.MaTrangThai switch
                    {
                        1 => "Đăng ký đang chờ duyệt, chưa thể trả phòng",
                        2 => "Đăng ký đã được duyệt nhưng chưa bắt đầu sử dụng phòng",
                        3 => "Đăng ký đã bị từ chối",
                        5 => "Đăng ký đã bị hủy",
                        7 => "Phòng đã được trả rồi",
                        _ => "Trạng thái không hợp lệ để trả phòng"
                    };
                    return (false, statusMessage);
                }

                var now = GetVietnamTime();

                // Tìm bản ghi sử dụng phòng
                var usage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
                if (usage == null)
                {
                    // Tạo bản ghi nếu không có
                    usage = new SuDungPhong
                    {
                        MaDangKy = maDangKy,
                        GioBatDauThucTe = booking.ThoiGianBatDau,
                        GioKetThucThucTe = now,
                        TinhTrangPhong = "Tốt",
                        GhiChu = "Trả phòng - Tạo bản ghi sử dụng tự động"
                    };
                    _context.SuDungPhongs.Add(usage);
                    _logger.LogWarning("Missing usage record for booking {MaDangKy}, creating automatically", maDangKy);
                }
                else
                {
                    // Cập nhật thời gian kết thúc thực tế
                    usage.GioKetThucThucTe = now;

                    if (string.IsNullOrEmpty(usage.TinhTrangPhong))
                        usage.TinhTrangPhong = "Tốt";

                    if (string.IsNullOrEmpty(usage.GhiChu))
                        usage.GhiChu = "Trả phòng bởi người dùng";
                    else if (!usage.GhiChu.Contains("Trả phòng"))
                        usage.GhiChu += " - Trả phòng bởi người dùng";
                }

                // Cập nhật trạng thái đăng ký từ 4 (sử dụng phòng) lên 7 (đã sử dụng)
                booking.MaTrangThai = 7;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully completed booking {MaDangKy} for user {UserId}", maDangKy, userId);

                // Tính toán thời gian sử dụng
                var actualStartTime = usage.GioBatDauThucTe ?? booking.ThoiGianBatDau;
                var usageDuration = now - actualStartTime;
                var isEarlyReturn = now < booking.ThoiGianKetThuc;

                // Tạo response message chi tiết
                var responseMessage = $"✅ Trả phòng thành công lúc {now:HH:mm dd/MM/yyyy}!\n" +
                        $"📊 Thời gian sử dụng: {usageDuration.TotalMinutes:0} phút\n" +
                     $"🏠 Tình trạng phòng: {usage.TinhTrangPhong}\n";

                if (isEarlyReturn)
                    responseMessage += $"🎉 Cảm ơn bạn đã trả phòng sớm {(booking.ThoiGianKetThuc - now).TotalMinutes:0} phút!";
                else
                    responseMessage += "⏰ Cảm ơn bạn đã sử dụng đúng thời gian!";

                return (true, responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing booking {MaDangKy} for user {UserId}", maDangKy, userId);
                return (false, "Lỗi khi cập nhật trạng thái hoàn thành. Vui lòng thử lại.");
            }
        }

        public async Task<(bool Success, string? Message)> CancelBookingAsync(int userId, CancelBookingRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(request.LyDoHuy))
                {
                    return (false, "Vui lòng nhập lý do hủy đăng ký.");
                }

                var booking = await _context.DangKyPhongs.FindAsync(request.MaDangKy);
                if (booking == null)
                    return (false, "Đăng ký không tồn tại.");

                if (booking.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền hủy đăng ký này.");

                // Chỉ cho phép hủy khi đang chờ duyệt (1) hoặc đã duyệt (2)
                if (booking.MaTrangThai != 1 && booking.MaTrangThai != 2)
                {
                    var statusMessage = booking.MaTrangThai switch
                    {
                        3 => "Đăng ký đã bị từ chối, không thể hủy",
                        4 => "Đăng ký đang được sử dụng, không thể hủy",
                        5 => "Đăng ký đã được hủy rồi",
                        7 => "Đăng ký đã hoàn thành, không thể hủy",
                        _ => "Không thể hủy đăng ký ở trạng thái hiện tại"
                    };
                    return (false, statusMessage);
                }

                var now = GetVietnamTime();

                // Không cho phép hủy nếu quá gần giờ bắt đầu (trong vòng 30 phút)
                if (booking.ThoiGianBatDau <= now.AddMinutes(30))
                    return (false, "Không thể hủy đăng ký trong vòng 30 phút trước giờ bắt đầu.");

                // Cập nhật trạng thái thành hủy (5) và lưu lý do hủy
                booking.MaTrangThai = 5;

                // Lưu lý do hủy vào cột GhiChu
                var lyDoHuyFull = $"[HỦY] {request.LyDoHuy}";
                if (!string.IsNullOrWhiteSpace(request.GhiChu))
                {
                    lyDoHuyFull += $" - Ghi chú: {request.GhiChu}";
                }

                // Cập nhật ghi chú với lý do hủy
                if (string.IsNullOrWhiteSpace(booking.GhiChu))
                {
                    booking.GhiChu = lyDoHuyFull;
                }
                else
                {
                    booking.GhiChu += " | " + lyDoHuyFull;
                }

                await _context.SaveChangesAsync();

                // Tạo thông báo về việc hủy đăng ký
                await CreateCancelNotificationAsync(userId, request, booking);

                _logger.LogInformation("Successfully cancelled booking {MaDangKy} for user {UserId} with reason: {Reason}",
                       request.MaDangKy, userId, request.LyDoHuy);

                return (true, $"Hủy đăng ký thành công. Lý do: {request.LyDoHuy}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {MaDangKy} for user {UserId}", request.MaDangKy, userId);
                return (false, "Lỗi hệ thống khi hủy đăng ký. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Hủy đặt phòng (phiên bản cũ - deprecated, dùng cho backward compatibility)
        /// </summary>
        public async Task<(bool Success, string? Message)> CancelBookingAsync(int userId, int maDangKy)
        {
         // Sử dụng phiên bản mới với lý do mặc định
            var request = new CancelBookingRequest
    {
  MaDangKy = maDangKy,
         LyDoHuy = "Người dùng hủy đăng ký", // Lý do mặc định
                GhiChu = "Hủy qua API cũ (không có lý do cụ thể)"
    };

          return await CancelBookingAsync(userId, request);
        }

        private async Task CreateCancelNotificationAsync(int userId, CancelBookingRequest request, DangKyPhong booking)
        {
            try
            {
                var title = "🚫 Đăng ký phòng đã được hủy";
                var content = $"Đăng ký phòng từ {booking.ThoiGianBatDau:dd/MM/yyyy HH:mm} đến {booking.ThoiGianKetThuc:dd/MM/yyyy HH:mm} đã được hủy.\n" +
      $"Lý do hủy: {request.LyDoHuy}";

                if (!string.IsNullOrWhiteSpace(request.GhiChu))
                {
                    content += $"\nGhi chú: {request.GhiChu}";
                }

                content += $"\nMã đăng ký: #{request.MaDangKy}";

                var thongBao = new ThongBao
                {
                    MaNguoiDung = userId,
                    TieuDe = title,
                    NoiDung = content,
                    NgayTao = GetVietnamTime(),
                    DaDoc = false
                };

                _context.ThongBaos.Add(thongBao);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created cancellation notification for user {UserId}, booking {MaDangKy}",
         userId, request.MaDangKy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create cancellation notification for booking {MaDangKy}",
        request.MaDangKy);
            }
        }
    }
}