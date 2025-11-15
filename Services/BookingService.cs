using Dapper;
using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Collections.Immutable;
using System.Data.SqlClient;
namespace HUIT_Library.Services
{
    public class BookingService : IBookingService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly IConfiguration _configuration;

        private const int DB_PENDING = 1;      // Chờ duyệt
        private const int DB_APPROVED = 2;     // Đã duyệt  
        private const int DB_REJECTED = 3;     // Từ chối
        private const int DB_INUSE = 4;   // Đang sử dụng
        private const int DB_CANCELLED = 5;    // Hủy
        private const int DB_USED = 6;         // Đã trả phòng

        // Logical booking statuses used throughout the app
        private enum BookingStatus
        {
            Pending = 1,    // Chờ duyệt
            Approved = 2,   // Đã duyệt
            Rejected = 3,   // Từ chối
            InUse = 4,      // Đang sử dụng
            Cancelled = 5,  // Hủy
            Used = 6        // Đã trả phòng
        }

        // Map DB values to normalized BookingStatus
        private BookingStatus MapDbToStatus(int? dbValue)
        {
            return dbValue switch
            {
                DB_PENDING => BookingStatus.Pending,
                DB_APPROVED => BookingStatus.Approved,
                DB_REJECTED => BookingStatus.Rejected,
                DB_INUSE => BookingStatus.InUse,
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
                BookingStatus.Rejected => "Từ chối",
                BookingStatus.InUse => "Đang sử dụng",
                BookingStatus.Cancelled => "Hủy",
                BookingStatus.Used => "Đã trả phòng",
                _ => "Không xác định"
            };
        }

        public BookingService(HuitThuVienContext context, IConfiguration configuration, ILogger<BookingService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        // Convert a DateTime that is provided in Vietnam local time (no offset) to UTC for storage


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

                    return (true, $"{resultMessage}");
                }
                else
                {
                    // Thất bại - log chi tiết và trả về thông báo thân thiện
                    _logger.LogWarning("Booking creation failed for user {UserId}. Code: {ResultCode}, Message: {ResultMessage}",
                        userId, resultCode, resultMessage);

                    return (false, $"{resultMessage}");
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
                NgayTao = GetVietnamTime(), // Sử dụng giờ Việt Nam
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting extension for booking {BookingId} to {NewEndTime}",
                    userId, request.MaDangKy, request.NewEndTime);

                // 1️⃣ Load booking và kiểm tra quyền
                var booking = await _context.DangKyPhongs.FindAsync(request.MaDangKy);
                if (booking == null)
                    return (false, "Yêu cầu đặt phòng không tồn tại.");

                if (booking.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền gia hạn yêu cầu này.");

                // 2️⃣ Kiểm tra trạng thái - chỉ cho phép gia hạn khi đang sử dụng (trạng thái 4)
                if (booking.MaTrangThai != DB_INUSE)
                {
                    var statusMessage = booking.MaTrangThai switch
                    {
                        DB_PENDING => "Đăng ký đang chờ duyệt, chưa thể gia hạn",
                        DB_APPROVED => "Đăng ký đã được duyệt nhưng chưa bắt đầu sử dụng",
                        DB_REJECTED => "Đăng ký đã bị từ chối",
                        DB_CANCELLED => "Đăng ký đã bị hủy",
                        DB_USED => "Đăng ký đã kết thúc, không thể gia hạn",
                        _ => "Trạng thái không hợp lệ để gia hạn"
                    };
                    return (false, statusMessage);
                }

                var now = GetVietnamTime();

                // 3️⃣ Kiểm tra thời gian - phải còn hơn 15 phút và đang trong thời gian sử dụng
                if (!(booking.ThoiGianBatDau <= now && booking.ThoiGianKetThuc > now))
                    return (false, "Phòng không đang trong thời gian sử dụng.");

                var remaining = booking.ThoiGianKetThuc - now;
                if (remaining.TotalMinutes < 15)
                    return (false, "Không thể gia hạn khi còn dưới 15 phút.");

                // 4️⃣ Kiểm tra thời gian gia hạn hợp lệ - tối đa 2 giờ
                if (request.NewEndTime <= booking.ThoiGianKetThuc)
                    return (false, "Thời gian kết thúc mới phải lớn hơn thời gian hiện tại.");

                var extension = request.NewEndTime - booking.ThoiGianKetThuc;
                if (extension <= TimeSpan.Zero || extension > TimeSpan.FromHours(2))
                    return (false, "Chỉ cho phép gia hạn tối đa 2 giờ.");

                // 5️⃣ Gọi stored procedure để kiểm tra xung đột 
                using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@MaPhong", booking.MaPhong);
                parameters.Add("@MaDangKyHienTai", request.MaDangKy);
                parameters.Add("@ThoiGianBatDauGiaHan", booking.ThoiGianKetThuc);
                parameters.Add("@ThoiGianKetThucMoi", request.NewEndTime);
                parameters.Add("@KetQua", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ThongBao", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                _logger.LogInformation("Calling sp_KiemTraGiaHanPhong for room {RoomId}, current booking {BookingId}",
                 booking.MaPhong, request.MaDangKy);

                await conn.ExecuteAsync("dbo.sp_KiemTraGiaHanPhong", parameters, commandType: CommandType.StoredProcedure);

                var ketQua = parameters.Get<int>("@KetQua");
                var thongBao = parameters.Get<string>("@ThongBao") ?? "";

                if (ketQua != 0)
                {
                    _logger.LogWarning("Extension failed for booking {BookingId}. Code: {Code}, Message: {Message}",
                      request.MaDangKy, ketQua, thongBao);
                    return (false, thongBao);
                }

                // 6️⃣ Cập nhật thời gian kết thúc và auto-approve
                booking.ThoiGianKetThuc = request.NewEndTime;
                booking.NgayDuyet = now;
                booking.NguoiDuyet = 0; // System auto-approve

                await _context.SaveChangesAsync();

                // 7️⃣ Tạo thông báo cho user
                var extensionMinutes = (int)extension.TotalMinutes;
                var thongBaoGiaHan = new ThongBao
                {
                    MaNguoiDung = userId,
                    TieuDe = "✅ Gia hạn phòng thành công",
                    NoiDung = $"Bạn đã gia hạn thành công phòng {booking.MaPhongNavigation?.TenPhong ?? "N/A"} " +
                             $"thêm {extensionMinutes} phút (đến {request.NewEndTime:HH:mm dd/MM/yyyy}). " +
               $"Mã đăng ký: #{request.MaDangKy}",
                    NgayTao = now,
                    DaDoc = false
                };

                _context.ThongBaos.Add(thongBaoGiaHan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully extended booking {BookingId} for user {UserId} by {Minutes} minutes",
              request.MaDangKy, userId, extensionMinutes);

                return (true, $"✅ Gia hạn thành công thêm {extensionMinutes} phút! Thời gian mới: {request.NewEndTime:HH:mm dd/MM/yyyy}");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during booking extension for booking {BookingId}", request.MaDangKy);
                return (false, "Lỗi hệ thống khi gia hạn. Vui lòng thử lại sau.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending booking {BookingId} for user {UserId}", request.MaDangKy, userId);
                return (false, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.");
            }
        }
        public async Task<(bool Success, string? Message)> CompleteBookingAsync(int userId, int maDangKy)
        {
            // Verify booking exists
            var booking = await _context.DangKyPhongs.FindAsync(maDangKy);
            if (booking == null)
                return (false, "Đăng ký không tồn tại.");

            if (booking.MaNguoiDung != userId)
                return (false, "Bạn không có quyền trả phòng cho đăng ký này.");

            // Kiểm tra trạng thái hiện tại - chỉ cho phép trả phòng khi đang sử dụng (trạng thái 4)
            if (booking.MaTrangThai != DB_INUSE)
            {
                var statusMessage = booking.MaTrangThai switch
                {
                    1 => "Đăng ký đang chờ duyệt, chưa thể trả phòng",
                    2 => "Đăng ký đã được duyệt nhưng chưa bắt đầu sử dụng phòng",
                    4 => "Đăng ký đã bị từ chối",
                    5 => "Đăng ký đã bị hủy",
                    6 => "Phòng đã được trả rồi",
                    _ => "Trạng thái không hợp lệ để trả phòng"
                };
                return (false, statusMessage);
            }

            var now = GetVietnamTime();

            // Tìm bản ghi sử dụng phòng (phải có vì đã ở trạng thái 4)
            var usage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
            if (usage == null)
            {
                // Tạo bản ghi nếu không có (trường hợp đặc biệt)
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

                // Cập nhật tình trạng phòng nếu chưa có
                if (string.IsNullOrEmpty(usage.TinhTrangPhong))
                    usage.TinhTrangPhong = "Tốt";

                // Cập nhật ghi chú
                if (string.IsNullOrEmpty(usage.GhiChu))
                    usage.GhiChu = "Trả phòng bởi người dùng";
                else if (!usage.GhiChu.Contains("Trả phòng"))
                    usage.GhiChu += " - Trả phòng bởi người dùng";
            }

            // Cập nhật trạng thái đăng ký từ 4 (đang sử dụng) lên 6 (đã trả phòng)
            booking.MaTrangThai = DB_USED;

            // Tính toán thời gian sử dụng
            var actualStartTime = usage.GioBatDauThucTe ?? booking.ThoiGianBatDau;
            var usageDuration = now - actualStartTime;
            var scheduledDuration = booking.ThoiGianKetThuc - booking.ThoiGianBatDau;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully completed booking {MaDangKy} for user {UserId} at {CompletionTime}",
   maDangKy, userId, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving completion for MaDangKy={MaDangKy}", maDangKy);
                return (false, "Lỗi khi cập nhật trạng thái hoàn thành. Vui lòng thử lại.");
            }

            // Tạo thông báo cảm ơn với thông tin chi tiết
            var reviewUrl = _configuration["Frontend:ReviewUrl"] ?? "https://frontend.example.com/review";
            var isEarlyReturn = now < booking.ThoiGianKetThuc;
            var timeInfo = isEarlyReturn ?
             $"Bạn đã trả phòng sớm {(booking.ThoiGianKetThuc - now).TotalMinutes:0} phút" :
               "Bạn đã trả phòng đúng giờ";

            var thongBao = new ThongBao
            {
                MaNguoiDung = userId,
                TieuDe = "✅ Trả phòng thành công",
                NoiDung = $"Bạn đã trả phòng thành công lúc {now:HH:mm dd/MM/yyyy}. {timeInfo}. " +
 $"Thời gian sử dụng thực tế: {usageDuration.TotalMinutes:0} phút. " +
            $"Cảm ơn bạn đã sử dụng dịch vụ! Đánh giá tại: {reviewUrl}?maDangKy={maDangKy}",
                NgayTao = now,
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created completion notification for user {UserId}, booking {MaDangKy}", userId, maDangKy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save completion notification for MaDangKy={MaDangKy}", maDangKy);
            }

            // Gửi email cảm ơn với thông tin chi tiết
            try
            {
                var user = await _context.NguoiDungs.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var reviewLinkBase = reviewUrl;
                    var reviewLink = $"{reviewLinkBase}?maDangKy={maDangKy}&maPhong={booking.MaPhong}";

                    var body = $@"
 <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
   <div style='text-align: center; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; color: white; margin-bottom: 30px;'>
            <h1 style='margin: 0; font-size: 28px;'>✅ Trả phòng thành công!</h1>
       <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>HUIT Library</p>
        </div>
            
            <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin-bottom: 25px;'>
                <p style='margin: 0 0 10px 0;'>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(user.HoTen ?? "")}</strong>,</p>
   <p style='margin: 0; color: #28a745; font-weight: 600;'>Bạn đã trả phòng thành công lúc {now:HH:mm dd/MM/yyyy} (Giờ Việt Nam)</p>
    {(isEarlyReturn ? $"<p style='margin: 5px 0 0 0; color: #17a2b8;'>🎉 Cảm ơn bạn đã trả phòng sớm {(booking.ThoiGianKetThuc - now).TotalMinutes:0} phút!</p>" : "")}
            </div>

  <div style='background: white; border: 1px solid #e9ecef; border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
        <h3 style='color: #495057; margin: 0 0 15px 0; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;'>📊 Thông tin chi tiết</h3>
   <table style='width: 100%; border-collapse: collapse;'>
  <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Mã đăng ký:</strong></td><td style='padding: 8px 0;'>#{maDangKy}</td></tr>
         <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Phòng:</strong></td><td style='padding: 8px 0;'>{booking.MaPhongNavigation?.TenPhong ?? "Chưa xác định"}</td></tr>
      <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Thời gian đặt:</strong></td><td style='padding: 8px 0;'>{booking.ThoiGianBatDau:HH:mm dd/MM/yyyy} - {booking.ThoiGianKetThuc:HH:mm dd/MM/yyyy}</td></tr>
          <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Thời gian thực tế:</strong></td><td style='padding: 8px 0;'>{actualStartTime:HH:mm dd/MM/yyyy} - {now:HH:mm dd/MM/yyyy}</td></tr>
        <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Thời gian sử dụng:</strong></td><td style='padding: 8px 0;'>{usageDuration.TotalMinutes:0} phút</td></tr>
  <tr><td style='padding: 8px 0; color: #6c757d;'><strong>Tình trạng phòng:</strong></td><td style='padding: 8px 0; color: #28a745;'>✅ {usage.TinhTrangPhong}</td></tr>
     </table>
  </div>
   
            <div style='text-align: center; background: #fff3cd; padding: 20px; border-radius: 8px; border: 1px solid #ffeaa7; margin-bottom: 25px;'>
           <h3 style='color: #856404; margin: 0 0 15px 0;'>🌟 Đánh giá trải nghiệm của bạn</h3>
       <p style='color: #856404; margin: 0 0 20px 0;'>Ý kiến của bạn giúp chúng tôi cải thiện chất lượng dịch vụ!</p>
           
                <div style='display: inline-block;'>
          <a href='{reviewLink}&type=room' style='display: inline-block; background: #28a745; color: white; padding: 12px 20px; text-decoration: none; border-radius: 25px; margin: 5px; font-weight: 600; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
   📍 Đánh giá phòng
    </a>
           <a href='{reviewLink}&type=service' style='display: inline-block; background: #17a2b8; color: white; padding: 12px 20px; text-decoration: none; border-radius: 25px; margin: 5px; font-weight: 600; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
   🛎️ Đánh giá dịch vụ  
            </a>
   <a href='{reviewLink}&type=staff' style='display: inline-block; background: #ffc107; color: #212529; padding: 12px 20px; text-decoration: none; border-radius: 25px; margin: 5px; font-weight: 600; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
        👥 Đánh giá nhân viên
           </a>
 </div>
            </div>
   
       <div style='text-align: center; color: #6c757d; padding: 20px; border-top: 1px solid #e9ecef;'>
             <p style='margin: 0 0 5px 0; font-style: italic;'>Cảm ơn bạn đã tin tướng và sử dụng dịch vụ của chúng tôi!</p>
             <p style='margin: 0; font-weight: 600; color: #495057;'>📚 HUIT Library Team</p>
    </div>
        </div>";

                    await SendEmailAsync(user.Email, "✅ Trả phòng thành công - Cảm ơn bạn!", body);
                    _logger.LogInformation("Sent completion email to user {UserId} at {Email}", userId, user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send completion email for MaDangKy={MaDangKy}", maDangKy);
            }

            // Tạo response message chi tiết
            var responseMessage = $"✅ Trả phòng thành công lúc {now:HH:mm dd/MM/yyyy} (Giờ Việt Nam)!\n" +
                   $"📊 Thời gian sử dụng: {usageDuration.TotalMinutes:0} phút\n" +
             $"🏠 Tình trạng phòng: {usage.TinhTrangPhong}\n";

            if (isEarlyReturn)
                responseMessage += $"🎉 Cảm ơn bạn đã trả phòng sớm {(booking.ThoiGianKetThuc - now).TotalMinutes:0} phút!";
            else
                responseMessage += "⏰ Cảm ơn bạn đã sử dụng đúng thời gian!";

            return (true, responseMessage);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var section = _configuration.GetSection("EmailSettings");
            var smtpHost = section["Host"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(section["Port"], out var p) ? p : 587;
            var enableSsl = bool.TryParse(section["EnableSSL"], out var ssl) ? ssl : true;
            var smtpUser = section["UserName"];
            var smtpPass = section["Password"];
            var fromAddress = section["From"] ?? smtpUser;

            if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogWarning("EmailSettings.UserName or Password is missing; will not send email.");
                throw new InvalidOperationException("EmailSettings are not configured (UserName/Password missing).");
            }

            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = enableSsl,
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromAddress ?? smtpUser, "HUIT Library"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            try
            {
                if (!string.IsNullOrWhiteSpace(smtpUser) && !mail.Bcc.Any())
                {
                    mail.Bcc.Add(smtpUser);
                }
            }
            catch { }

            try
            {
                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation("Thank-you email sent to {To}", toEmail);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error when sending email to {To}", toEmail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when sending email to {To}", toEmail);
                throw;
            }
        }

        public async Task<List<BookingHistoryDto>> GetBookingHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting booking history for user {UserId}, page {PageNumber}, size {PageSize}",
      userId, pageNumber, pageSize);

                // Lấy lịch sử đặt phòng của user (bao gồm đã từ chối(4), huỷ(5), đã sử dụng(6))
                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            join suDung in _context.SuDungPhongs on dk.MaDangKy equals suDung.MaDangKy into suDungGroup
                            from sd in suDungGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && (dk.MaTrangThai == 3 || dk.MaTrangThai == 5 || dk.MaTrangThai == 6)
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
                var totalCount = await query.CountAsync();
                var result = await query
              .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                     .ToListAsync();

                _logger.LogInformation("Found {Count} booking history records for user {UserId} (total: {Total})",
                result.Count, userId, totalCount);

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

                // Lấy các đăng ký hiện tại với trạng thái mới:
                // - Luôn lấy các bản ghi đang sử dụng (DB_INUSE = 4)
                // - Lấy các bản ghi chờ duyệt (DB_PENDING = 1) hoặc đã duyệt (DB_APPROVED = 2)
                //   chỉ khi chưa quá thời gian kết thúc
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

                    // ❌ KHÔNG convert UTC → VN nữa
                    var start = dk.ThoiGianBatDau;
                    var end = dk.ThoiGianKetThuc;

                    // Tính toán thời gian
                    var minutesUntilStart = (int)(start - nowVn).TotalMinutes;
                    var minutesRemaining = (int)(end - nowVn).TotalMinutes;

                    // Actions với logic mới
                    var canStart = normalizedStatus == BookingStatus.Approved &&
                        minutesUntilStart <= 15 && minutesUntilStart >= -5;

                    var canExtend = normalizedStatus == BookingStatus.InUse &&
                     minutesRemaining > 15 && nowVn >= start && nowVn <= end;

                    var canComplete = normalizedStatus == BookingStatus.InUse;

                    // Status description với trạng thái mới
                    string statusDescription = normalizedStatus switch
                    {
                        BookingStatus.Pending => minutesUntilStart > 0 ? $"Chờ duyệt - Bắt đầu sau {minutesUntilStart} phút" : "Chờ duyệt - Đã đến giờ",
                        BookingStatus.Approved when minutesUntilStart > 15 => $"Đã duyệt - Có thể checkin sau {minutesUntilStart - 15} phút",
                        BookingStatus.Approved when minutesUntilStart <= 15 && minutesUntilStart > 0 => "Đã duyệt - Có thể checkin ngay",
                        BookingStatus.Approved when minutesUntilStart <= 0 && minutesRemaining > 0 => "Đã duyệt - Đã đến giờ, có thể checkin",
                        BookingStatus.Approved when minutesRemaining <= 0 => "Đã duyệt - Đã quá giờ",
                        BookingStatus.InUse when minutesRemaining > 15 => $"Đang sử dụng - Còn {minutesRemaining} phút (có thể gia hạn)",
                        BookingStatus.InUse when minutesRemaining <= 15 && minutesRemaining > 0 => $"Đang sử dụng - Còn {minutesRemaining} phút (không thể gia hạn)",
                        BookingStatus.InUse when minutesRemaining <= 0 => "Đang sử dụng - Đã quá giờ, cần trả phòng",
                        BookingStatus.Rejected => "Đã từ chối",
                        BookingStatus.Cancelled => "Đã hủy",
                        BookingStatus.Used => "Đã trả phòng",
                        _ => GetStatusName(normalizedStatus)
                    };

                    return new CurrentBookingDto
                    {
                        MaDangKy = dk.MaDangKy,
                        MaPhong = dk.MaPhong ?? 0,
                        TenPhong = p != null ? p.TenPhong : "Chưa phân phòng",
                        TenLoaiPhong = loaiPhong.TenLoaiPhong,
                        ThoiGianBatDau = dk.ThoiGianBatDau,
                        ThoiGianKetThuc = dk.ThoiGianKetThuc,
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

                _logger.LogInformation("Found {Count} current bookings for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current bookings for user {UserId}", userId);
                return new List<CurrentBookingDto>();
            }
        }
    }
}