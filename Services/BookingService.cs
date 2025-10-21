using Dapper;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace HUIT_Library.Services
{
    public class BookingService : IBookingService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly IConfiguration _configuration;

        public BookingService(HuitThuVienContext context, IConfiguration configuration, ILogger<BookingService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string? Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request)
        {
            // Basic validation before calling stored procedure
            if (request.MaLoaiPhong <= 0)
                return (false, "MaLoaiPhong không hợp lệ.");

            if (request.ThoiGianKetThuc <= request.ThoiGianBatDau)
                return (false, "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

            // Optional: require start time in the future (allow small clock skew)
            var now = DateTime.UtcNow;
            if (request.ThoiGianBatDau.ToUniversalTime() < now.AddMinutes(-5))
                return (false, "Thời gian bắt đầu phải là thời điểm hiện tại hoặc trong tương lai.");

            // Use stored procedure sp_DangKyPhong to find and insert a suitable room
            await using var conn = _context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@MaNguoiDung", userId, DbType.Int32);
            parameters.Add("@MaLoaiPhong", request.MaLoaiPhong, DbType.Int32);
            parameters.Add("@ThoiGianBatDau", request.ThoiGianBatDau, DbType.DateTime);
            parameters.Add("@ThoiGianKetThuc", request.ThoiGianKetThuc, DbType.DateTime);
            parameters.Add("@GhiChu", request.LyDo, DbType.String);

            try
            {
                _logger.LogInformation("Calling sp_DangKyPhong: MaNguoiDung={UserId}, MaLoaiPhong={MaLoaiPhong}, ThoiGianBatDau={Start}, ThoiGianKetThuc={End}",
                    userId, request.MaLoaiPhong, request.ThoiGianBatDau, request.ThoiGianKetThuc);

                // Execute stored procedure. The proc may PRINT or set NOCOUNT ON; ExecuteAsync returns rows affected for INSERT but can be 0 even if insert occurred.
                var rows = await conn.ExecuteAsync("dbo.sp_DangKyPhong", parameters, commandType: CommandType.StoredProcedure);

                _logger.LogInformation("sp_DangKyPhong returned rowsAffected={Rows}", rows);

                if (rows > 0)
                {
                    // Try to find inserted record to include details in notification
                    try
                    {
                        var inserted = await _context.DangKyPhongs
                            .Where(d => d.MaNguoiDung == userId && d.ThoiGianBatDau == request.ThoiGianBatDau && d.ThoiGianKetThuc == request.ThoiGianKetThuc)
                            .OrderByDescending(d => d.MaDangKy)
                            .FirstOrDefaultAsync();

                        await CreateNotificationForBookingAsync(userId, request, inserted?.MaDangKy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create notification after successful sp_DangKyPhong (rows>0).");
                    }

                    return (true, "Yêu cầu mượn phòng đã được gửi, vui lòng chờ duyệt.");
                }

                // If stored proc returns 0 rows, verify whether a matching DangKyPhong record was inserted.
                try
                {
                    var inserted = await _context.DangKyPhongs
                        .Where(d => d.MaNguoiDung == userId && d.ThoiGianBatDau == request.ThoiGianBatDau && d.ThoiGianKetThuc == request.ThoiGianKetThuc)
                        .OrderByDescending(d => d.MaDangKy)
                        .FirstOrDefaultAsync();

                    if (inserted != null)
                    {
                        _logger.LogInformation("Detected inserted DangKyPhong (MaDangKy={MaDangKy}) despite sp returning 0 rows.", inserted.MaDangKy);

                        try
                        {
                            await CreateNotificationForBookingAsync(userId, request, inserted.MaDangKy);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create notification after detecting inserted record.");
                        }

                        return (true, "Yêu cầu mượn phòng đã được gửi, vui lòng chờ duyệt.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while verifying inserted record after sp_DangKyPhong returned 0 rows.");
                }

                // If not found, report likely conflict
                return (false, "Không thể đăng ký phòng: có thể không có phòng trống hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra lịch hoặc tham số gửi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling sp_DangKyPhong for MaNguoiDung={UserId}", userId);
                // Log exception if logger is available. For now return a generic error message.
                return (false, "Lỗi hệ thống khi gọi stored procedure. Vui lòng thử lại.");
            }
        }

        private async Task CreateNotificationForBookingAsync(int userId, CreateBookingRequest request, int? maDangKy)
        {
            var title = "Yêu cầu mượn phòng đã được gửi";
            var content = $"Yêu cầu mượn phòng (Loại: {request.MaLoaiPhong}) từ {request.ThoiGianBatDau:u} đến {request.ThoiGianKetThuc:u} đã được gửi. Vui lòng chờ duyệt.";
            if (maDangKy.HasValue)
            {
                content += $" (Mã đăng ký: {maDangKy.Value})";
            }

            var thongBao = new ThongBao
            {
                MaNguoiDung = userId,
                TieuDe = title,
                NoiDung = content,
                NgayTao = DateTime.UtcNow,
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request)
        {
            // Load booking
            var booking = await _context.DangKyPhongs.FindAsync(request.MaDangKy);
            if (booking == null) return (false, "Yêu cầu không tồn tại.");
            if (booking.MaNguoiDung != userId) return (false, "Bạn không có quyền gia hạn yêu cầu này.");

            // Must be currently in use
            var now = DateTime.UtcNow;
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

            // Also check LichTrangThaiPhong schedules for the additional period (date may cross days)
            var startDate = booking.ThoiGianKetThuc.Date;
            var endDate = request.NewEndTime.Date;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                var dateOnly = DateOnly.FromDateTime(dt);
                var start = dt == startDate ? booking.ThoiGianKetThuc.TimeOfDay : TimeSpan.Zero;
                var end = dt == endDate ? request.NewEndTime.TimeOfDay : TimeSpan.FromHours(24);

                var scheduleConflict = await _context.LichTrangThaiPhongs
                    .Where(l => l.MaPhong == booking.MaPhong && l.Ngay == dateOnly &&
                                !(end <= l.GioBatDau.ToTimeSpan() || start >= l.GioKetThuc.ToTimeSpan()))
                    .AnyAsync();

                if (scheduleConflict)
                    return (false, "Gia hạn thất bại. Phòng đã được đặt trước.");
            }

            // Auto-approve and update end time
            booking.ThoiGianKetThuc = request.NewEndTime;
            booking.NgayDuyet = DateTime.UtcNow;
            booking.NguoiDuyet = 0; // system
            booking.MaTrangThai = 2; // assume 2 = approved

            try
            {
                await _context.SaveChangesAsync();
                return (true, "Gia hạn thành công.");
            }
            catch
            {
                return (false, "Lỗi hệ thống. Vui lòng thử lại.");
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

            // Find or create SuDungPhong record
            var usage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
            if (usage == null)
            {
                usage = new SuDungPhong
                {
                    MaDangKy = maDangKy,
                    GioBatDauThucTe = booking.ThoiGianBatDau,
                    GioKetThucThucTe = DateTime.UtcNow,
                    TinhTrangPhong = null,
                    GhiChu = "Hoàn thành bởi người dùng"
                };
                _context.SuDungPhongs.Add(usage);
            }
            else
            {
                if (usage.GioKetThucThucTe == null)
                    usage.GioKetThucThucTe = DateTime.UtcNow;
            }

            // Optionally update DatPhong or booking status - set MaTrangThai = completed (e.g., 3)
            booking.MaTrangThai = 3;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving completion for MaDangKy={MaDangKy}", maDangKy);
                return (false, "Lỗi khi cập nhật trạng thái hoàn thành.");
            }

            // Create a thank-you notification with review link
            var reviewUrl = _configuration["Frontend:ReviewUrl"] ?? "https://frontend.example.com/review";
            var thongBao = new ThongBao
            {
                MaNguoiDung = userId,
                TieuDe = "Cảm ơn bạn đã sử dụng dịch vụ",
                NoiDung = $"Cảm ơn bạn đã sử dụng phòng. Vui lòng đánh giá trải nghiệm của bạn tại: {reviewUrl}?maDangKy={maDangKy}",
                NgayTao = DateTime.UtcNow,
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save thank-you notification for MaDangKy={MaDangKy}", maDangKy);
            }

            // Send thank-you email with links to review room, service and staff
            try
            {
                var user = await _context.NguoiDungs.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var reviewLinkBase = reviewUrl;
                    var reviewLink = $"{reviewLinkBase}?maDangKy={maDangKy}&maPhong={booking.MaPhong}";

                    var body = $@"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.HoTen ?? "")},</p>
                    <p>Cảm ơn bạn đã sử dụng phòng. Chúng tôi rất mong nhận được phản hồi của bạn về:</p>
                    <ul>
                    <li><a href='{reviewLink}&type=room'>Đánh giá phòng</a></li>
                    <li><a href='{reviewLink}&type=service'>Đánh giá dịch vụ</a></li>
                    <li><a href='{reviewLink}&type=staff'>Đánh giá thái độ nhân viên</a></li>
                    </ul>
                    <p>Cảm ơn bạn đã dành thời gian!</p>";

                    await SendEmailAsync(user.Email, "Cảm ơn đã sử dụng dịch vụ - Đánh giá trải nghiệm", body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send thank-you email for MaDangKy={MaDangKy}", maDangKy);
            }

            return (true, "Trả phòng thành công. Cảm ơn bạn đã sử dụng dịch vụ.");
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
    }
}