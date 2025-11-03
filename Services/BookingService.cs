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

        // Helper method to get Vietnam timezone
        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        public async Task<(bool Success, string? Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request)
        {
            // 1️⃣ Kiểm tra cơ bản
            if (request.MaLoaiPhong <= 0)
                return (false, "Mã loại phòng không hợp lệ.");

            if (request.ThoiGianBatDau == default)
                return (false, "Thời gian bắt đầu không hợp lệ.");
            
            var now = GetVietnamTime(); // Sử dụng giờ Việt Nam
            if (request.ThoiGianBatDau < now.AddMinutes(-5))
                return (false, "Thời gian bắt đầu phải là hiện tại hoặc trong tương lai.");

            // 2️⃣ Mở kết nối DB
            await using var conn = _context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();

            // 3️⃣ Chuẩn bị tham số tương ứng với sp_DangKyPhong
            var parameters = new DynamicParameters();
            parameters.Add("@MaNguoiDung", userId, DbType.Int32);
            parameters.Add("@MaLoaiPhong", request.MaLoaiPhong, DbType.Int32);
            parameters.Add("@ThoiGianBatDau", request.ThoiGianBatDau, DbType.DateTime);
            parameters.Add("@LyDo", request.LyDo, DbType.String);
            parameters.Add("@SoLuong", request.SoLuong > 0 ? request.SoLuong : 1, DbType.Int32);
            parameters.Add("@GhiChu", request.GhiChu, DbType.String);

            try
            {
                _logger.LogInformation(
                    "Calling sp_DangKyPhong: MaNguoiDung={UserId}, MaLoaiPhong={MaLoaiPhong}, ThoiGianBatDau={Start}",
                    userId, request.MaLoaiPhong, request.ThoiGianBatDau);

                // 4️⃣ Gọi stored procedure
                var rows = await conn.ExecuteAsync("dbo.sp_DangKyPhong", parameters, commandType: CommandType.StoredProcedure);

                _logger.LogInformation("sp_DangKyPhong returned rowsAffected={Rows}", rows);

                // 5️⃣ Kiểm tra insert thành công
                if (rows > 0)
                {
                    try
                    {
                        var endTime = request.ThoiGianBatDau.AddHours(2);

                        var inserted = await _context.DangKyPhongs
                            .Where(d => d.MaNguoiDung == userId &&
                                        d.ThoiGianBatDau == request.ThoiGianBatDau &&
                                        d.ThoiGianKetThuc == endTime)
                            .OrderByDescending(d => d.MaDangKy)
                            .FirstOrDefaultAsync();

                        if (inserted != null)
                            await CreateNotificationForBookingAsync(userId, request, inserted.MaDangKy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create notification after successful sp_DangKyPhong (rows>0).");
                    }

                    return (true, "Yêu cầu mượn phòng đã được gửi, vui lòng chờ duyệt.");
                }

                // 6️⃣ Nếu rows=0, thử tìm bản ghi vừa thêm (phòng hợp lệ nhưng SP chỉ PRINT)
                try
                {
                    var endTime = request.ThoiGianBatDau.AddHours(2);

                    var inserted = await _context.DangKyPhongs
                        .Where(d => d.MaNguoiDung == userId &&
                                    d.ThoiGianBatDau == request.ThoiGianBatDau &&
                                    d.ThoiGianKetThuc == endTime)
                        .OrderByDescending(d => d.MaDangKy)
                        .FirstOrDefaultAsync();

                    if (inserted != null)
                    {
                        _logger.LogInformation(
                            "Detected inserted DangKyPhong (MaDangKy={MaDangKy}) despite sp returning 0 rows.",
                            inserted.MaDangKy);

                        await CreateNotificationForBookingAsync(userId, request, inserted.MaDangKy);
                        return (true, "Yêu cầu mượn phòng đã được gửi, vui lòng chờ duyệt.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while verifying inserted record after sp_DangKyPhong returned 0 rows.");
                }

                // 7️⃣ Không có kết quả
                return (false, "Không thể đăng ký phòng: có thể không có phòng trống hoặc dữ liệu không hợp lệ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling sp_DangKyPhong for MaNguoiDung={UserId}", userId);
                return (false, $"Lỗi hệ thống khi gọi stored procedure: {ex.Message}");
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
            // Load booking
            var booking = await _context.DangKyPhongs.FindAsync(request.MaDangKy);
            if (booking == null) return (false, "Yêu cầu không tồn tại.");
            if (booking.MaNguoiDung != userId) return (false, "Bạn không có quyền gia hạn yêu cầu này.");

            // Must be currently in use
            var now = GetVietnamTime(); // Sử dụng giờ Việt Nam
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
            booking.NgayDuyet = GetVietnamTime(); // Sử dụng giờ Việt Nam
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

            // Kiểm tra trạng thái hiện tại - chỉ cho phép trả phòng khi đang sử dụng (trạng thái 3)
    if (booking.MaTrangThai != 3)
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

   var now = GetVietnamTime(); // Sử dụng giờ Việt Nam
            
       // Tìm bản ghi sử dụng phòng (phải có vì đã ở trạng thái 3)
            var usage = await _context.SuDungPhongs.FirstOrDefaultAsync(s => s.MaDangKy == maDangKy);
       if (usage == null)
  {
    // Tạo bản ghi nếu không có (trường hợp đặc biệt)
   usage = new SuDungPhong
      {
      MaDangKy = maDangKy,
   GioBatDauThucTe = booking.ThoiGianBatDau, // Thời gian dự kiến
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

        // Cập nhật trạng thái đăng ký từ 3 (sử dụng phòng) lên 6 (đã sử dụng)
    booking.MaTrangThai = 6;

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

                // Lấy lịch sử đặt phòng của user (chỉ các phòng đã sử dụng - trạng thái 6)
                var query = from dk in _context.DangKyPhongs
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            join loaiPhong in _context.LoaiPhongs on dk.MaLoaiPhong equals loaiPhong.MaLoaiPhong
                            join trangThai in _context.TrangThaiDangKies on dk.MaTrangThai equals trangThai.MaTrangThai into trangThaiGroup
                            from tt in trangThaiGroup.DefaultIfEmpty()
                            join suDung in _context.SuDungPhongs on dk.MaDangKy equals suDung.MaDangKy into suDungGroup
                            from sd in suDungGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && dk.MaTrangThai == 6 // Chỉ lấy trạng thái "đã sử dụng"
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
        
    }
}