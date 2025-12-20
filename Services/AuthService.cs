using Dapper;
using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Response;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HUIT_Library.Services
{
    public class AuthService : IAuthService
    {
        private readonly HuitThuVienContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ILogger<AuthService> _logger;
        private readonly IAuthNotificationService _authNotificationService;

        public AuthService(
            HuitThuVienContext context, 
            IConfiguration configuration, 
            IPasswordHashService passwordHashService, 
            ILogger<AuthService> logger,
            IAuthNotificationService authNotificationService)
        {
            _context = context;
            _configuration = configuration;
            _passwordHashService = passwordHashService;
            _logger = logger;
            _authNotificationService = authNotificationService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Use Dapper to fetch user and related records using the DbConnection from the DbContext
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                var nguoiDungTable = _context.Model.FindEntityType(typeof(NguoiDung))?.GetTableName() ?? "NguoiDung";
                var sinhVienTable = _context.Model.FindEntityType(typeof(SinhVien))?.GetTableName() ?? "SinhVien";
                var giangVienTable = _context.Model.FindEntityType(typeof(GiangVien))?.GetTableName() ?? "GiangVien";
                var nhanVienTable = _context.Model.FindEntityType(typeof(NhanVienThuVien))?.GetTableName() ?? "NhanVienThuVien";
                var quanLyTable = _context.Model.FindEntityType(typeof(QuanLyKyThuat))?.GetTableName() ?? "QuanLyKyThuat";
                var vaiTroTable = _context.Model.FindEntityType(typeof(VaiTro))?.GetTableName() ?? "VaiTro";

                var sql = $"SELECT * FROM [{nguoiDungTable}] WHERE MaDangNhap = @MaDangNhap";
                var user = (await conn.QueryAsync<NguoiDung>(sql, new { MaDangNhap = request.MaDangNhap })).FirstOrDefault();

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Verify password
                //if (!_passwordHashService.VerifyPassword(request.MatKhau, user.MatKhau ?? ""))
                //{
                //    return new LoginResponse
                //    {
                //        Success = false,
                //        Message = "Thông tin đăng nhập không chính xác!"
                //    };
                //}

                // Load related records (SinhVien, GiangVien, NhanVienThuVien, QuanLyKyThuat, VaiTro)
                var svSql = $"SELECT * FROM [{sinhVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.SinhVien = (await conn.QueryAsync<SinhVien>(svSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var gvSql = $"SELECT * FROM [{giangVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.GiangVien = (await conn.QueryAsync<GiangVien>(gvSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var nvSql = $"SELECT * FROM [{nhanVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.NhanVienThuVien = (await conn.QueryAsync<NhanVienThuVien>(nvSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var qlSql = $"SELECT * FROM [{quanLyTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.QuanLyKyThuat = (await conn.QueryAsync<QuanLyKyThuat>(qlSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                if (user.MaVaiTro != 0)
                {
                    var vtSql = $"SELECT * FROM [{vaiTroTable}] WHERE MaVaiTro = @MaVaiTro";
                    user.MaVaiTroNavigation = (await conn.QueryAsync<VaiTro>(vtSql, new { MaVaiTro = user.MaVaiTro })).FirstOrDefault();
                }

                // Xác định mã vai trò dựa vào bảng liên quan (scaffolded DB không có MaCode)
                string roleCode;
                if (user.SinhVien != null)
                    roleCode = "SINH_VIEN";
                else if (user.GiangVien != null)
                    roleCode = "GIANG_VIEN";
                else if (user.QuanLyKyThuat != null)
                    roleCode = "QUAN_TRI";
                else if (user.NhanVienThuVien != null)
                    roleCode = "NHAN_VIEN";
                else
                    roleCode = user.MaVaiTroNavigation?.TenVaiTro ?? string.Empty;

                // Chỉ cho phép GIANG_VIEN và SINH_VIEN đăng nhập qua API này
                if (roleCode != "GIANG_VIEN" && roleCode != "SINH_VIEN")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập hệ thống này!"
                    };
                }

                // ✅ TẠO SESSION MỚI VÀ GIỚI HẠN 3 THIẾT BỊ
                var deviceInfo = request.DeviceInfo ?? "Unknown Device";
                var ipAddress = request.IpAddress ?? "Unknown IP";
                
                var sessionResult = await CreateUserSessionAsync(user.MaNguoiDung, deviceInfo, ipAddress);
                if (!sessionResult.Success)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = sessionResult.Message
                    };
                }

                // ✅ TẠO ACCESS TOKEN (Thời hạn ngắn - 7 ngày, có thể refresh)
                var accessToken = GenerateJwtToken(user, roleCode, sessionResult.SessionId);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = accessToken,
                    RefreshToken = sessionResult.RefreshToken, // ✅ Trả về refresh token
                    User = new UserInfo
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        MaCode = user.MaDangNhap,
                        MaSinhVien = user.SinhVien?.MaSinhVien,
                        MaNhanVien = user.NhanVienThuVien?.MaNhanVien ?? user.GiangVien?.MaGiangVien,
                        HoTen = user.HoTen,
                        Email = user.Email,
                        SoDienThoai = user.SoDienThoai,
                        VaiTro = roleCode,
                        Lop = user.SinhVien?.Lop,
                        KhoaHoc = user.SinhVien?.Khoa,
                        NgaySinh = null
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {MaDangNhap}", request.MaDangNhap);
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> AdminLoginAsync(LoginRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                var nguoiDungTable = _context.Model.FindEntityType(typeof(NguoiDung))?.GetTableName() ?? "NguoiDung";
                var nhanVienTable = _context.Model.FindEntityType(typeof(NhanVienThuVien))?.GetTableName() ?? "NhanVienThuVien";
                var quanLyTable = _context.Model.FindEntityType(typeof(QuanLyKyThuat))?.GetTableName() ?? "QuanLyKyThuat";
                var giangVienTable = _context.Model.FindEntityType(typeof(GiangVien))?.GetTableName() ?? "GiangVien";
                var sinhVienTable = _context.Model.FindEntityType(typeof(SinhVien))?.GetTableName() ?? "SinhVien";
                var vaiTroTable = _context.Model.FindEntityType(typeof(VaiTro))?.GetTableName() ?? "VaiTro";

                var sql = $"SELECT * FROM [{nguoiDungTable}] WHERE MaDangNhap = @MaDangNhap";
                var user = (await conn.QueryAsync<NguoiDung>(sql, new { MaDangNhap = request.MaDangNhap })).FirstOrDefault();

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Load related records
                var nvSql = $"SELECT * FROM [{nhanVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.NhanVienThuVien = (await conn.QueryAsync<NhanVienThuVien>(nvSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var qlSql = $"SELECT * FROM [{quanLyTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.QuanLyKyThuat = (await conn.QueryAsync<QuanLyKyThuat>(qlSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var gvSql = $"SELECT * FROM [{giangVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.GiangVien = (await conn.QueryAsync<GiangVien>(gvSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                var svSql = $"SELECT * FROM [{sinhVienTable}] WHERE MaNguoiDung = @MaNguoiDung";
                user.SinhVien = (await conn.QueryAsync<SinhVien>(svSql, new { MaNguoiDung = user.MaNguoiDung })).FirstOrDefault();

                if (user.MaVaiTro != 0)
                {
                    var vtSql = $"SELECT * FROM [{vaiTroTable}] WHERE MaVaiTro = @MaVaiTro";
                    user.MaVaiTroNavigation = (await conn.QueryAsync<VaiTro>(vtSql, new { MaVaiTro = user.MaVaiTro })).FirstOrDefault();
                }

                // Xác định mã vai trò
                string roleCode;
                if (user.QuanLyKyThuat != null)
                    roleCode = "QUAN_TRI";
                else if (user.NhanVienThuVien != null)
                    roleCode = "NHAN_VIEN";
                else if (user.GiangVien != null)
                    roleCode = "GIANG_VIEN";
                else if (user.SinhVien != null)
                    roleCode = "SINH_VIEN";
                else
                    roleCode = user.MaVaiTroNavigation?.TenVaiTro ?? string.Empty;

                // Chỉ cho phép QUAN_TRI và NHAN_VIEN đăng nhập qua API này
                if (roleCode != "QUAN_TRI" && roleCode != "NHAN_VIEN")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập hệ thống quản lý!"
                    };
                }

                // ✅ TẠO SESSION MỚI VÀ GIỚI HẠN 3 THIẾT BỊ
                var deviceInfo = request.DeviceInfo ?? "Admin Device";
                var ipAddress = request.IpAddress ?? "Unknown IP";
                
                var sessionResult = await CreateUserSessionAsync(user.MaNguoiDung, deviceInfo, ipAddress);
                if (!sessionResult.Success)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = sessionResult.Message
                    };
                }

                var accessToken = GenerateJwtToken(user, roleCode, sessionResult.SessionId);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = accessToken,
                    RefreshToken = sessionResult.RefreshToken,
                    User = new UserInfo
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        MaCode = user.MaDangNhap,
                        MaSinhVien = user.SinhVien?.MaSinhVien,
                        MaNhanVien = user.NhanVienThuVien?.MaNhanVien ?? user.GiangVien?.MaGiangVien,
                        HoTen = user.HoTen,
                        Email = user.Email,
                        SoDienThoai = user.SoDienThoai,
                        VaiTro = roleCode,
                        Lop = user.SinhVien?.Lop,
                        KhoaHoc = user.SinhVien?.Khoa,
                        NgaySinh = null
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for user {MaDangNhap}", request.MaDangNhap);
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        // ✅ TẠO SESSION MỚI VÀ GIỚI HẠN 3 THIẾT BỊ
        private async Task<(bool Success, string Message, string? RefreshToken, int SessionId)> CreateUserSessionAsync(
            int userId, string deviceInfo, string ipAddress)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                // ✅ Gọi stored procedure để kiểm tra và giới hạn số thiết bị
                var parameters = new DynamicParameters();
                parameters.Add("@MaNguoiDung", userId);
                parameters.Add("@MaxSessions", 3);

                await conn.ExecuteAsync("sp_CheckAndLimitUserSessions", parameters, 
                    commandType: CommandType.StoredProcedure);

                // ✅ Tạo refresh token vĩnh viễn (hoặc thời hạn dài)
                var refreshToken = GenerateRefreshToken();
                var now = DateTime.Now;

                var session = new UserSession
                {
                    MaNguoiDung = userId,
                    RefreshToken = refreshToken,
                    DeviceInfo = deviceInfo,
                    IpAddress = ipAddress,
                    CreatedAt = now,
                    ExpiresAt = null, // ✅ NULL = vĩnh viễn
                    LastAccessAt = now,
                    IsRevoked = false
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Created new session {SessionId} for user {UserId} on device {DeviceInfo}", 
                    session.Id, userId, deviceInfo);

                return (true, "Session created successfully", refreshToken, session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user session for user {UserId}", userId);
                return (false, "Không thể tạo phiên đăng nhập. Vui lòng thử lại.", null, 0);
            }
        }

        // ✅ GENERATE JWT TOKEN (Thời hạn ngắn - có thể refresh)
        public string GenerateJwtToken(NguoiDung user, string role, int sessionId = 0)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "P6n@8X9z#A1k$F3q*L7v!R2y^C5m&E0w";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new(ClaimTypes.Name, user.HoTen),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Role, role),
                new("MaCode", user.MaDangNhap),
                new("VaiTro", role),
                new("SessionId", sessionId.ToString()) // ✅ Thêm SessionId để tracking
            };

            var maSinh = user.SinhVien?.MaSinhVien;
            if (!string.IsNullOrEmpty(maSinh))
                claims.Add(new Claim("MaSinhVien", maSinh));

            var maNhan = user.NhanVienThuVien?.MaNhanVien ?? user.GiangVien?.MaGiangVien;
            if (!string.IsNullOrEmpty(maNhan))
                claims.Add(new Claim("MaNhanVien", maNhan));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // ✅ Access token: 7 ngày
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "HUIT_Library",
                Audience = _configuration["Jwt:Audience"] ?? "HUIT_Library_Users"
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // ✅ GENERATE REFRESH TOKEN (Random secure string)
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        ///////////////////////////////////////////////////////

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return new ForgotPasswordResponse { Success = false, Message = "Email không tồn tại trong hệ thống.", EmailSent = false };

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var nowVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // 🔒 Kiểm tra số lần gửi trong 15 phút qua
            var fifteenMinutesAgo = nowVietnam.AddMinutes(-15);
            var recentRequests = await _context.PasswordResetTokens
                .CountAsync(t => t.MaNguoiDung == user.MaNguoiDung && t.ExpireAt >= fifteenMinutesAgo);

            if (recentRequests >= 3)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Bạn đã yêu cầu đặt lại mật khẩu quá nhiều lần. Vui lòng thử lại sau 15 phút.",
                    EmailSent = false
                };
            }

            // 🔑 Tạo token
            var token = Guid.NewGuid().ToString("N");
            var expireAtVietnam = nowVietnam.AddMinutes(5);

            var resetToken = new PasswordResetToken
            {
                MaNguoiDung = user.MaNguoiDung,
                Token = token,
                ExpireAt = expireAtVietnam,
                Used = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var frontendUrl = _configuration["Frontend:ResetPasswordUrl"] ?? "http://localhost:4200/reset-password";
            var resetLink = $"{frontendUrl}?token={token}";
            var body = $"<p>Nhấn vào link để đặt lại mật khẩu (hiệu lực trong 5 phút): <a href='{resetLink}'>Đặt lại ngay</a></p>";

            var emailSent = false;
            try
            {
                await SendEmailAsync(user.Email, "Đặt lại mật khẩu", body);
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailSent = false;
                _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = emailSent
                    ? "Email đặt lại mật khẩu đã được gửi."
                    : "Token đã được tạo nhưng email chưa được gửi do cấu hình SMTP chưa đầy đủ.",
                EmailSent = emailSent
            };
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && (t.Used == null || t.Used == false));

            if (resetToken == null)
                return false;

            // Chuyển UTC -> giờ Việt Nam
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var nowVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Kiểm tra hạn
            if (resetToken.ExpireAt < nowVietnam)
                return false;

            var user = await _context.NguoiDungs.FindAsync(resetToken.MaNguoiDung);
            if (user == null) return false;

            user.MatKhau = _passwordHashService.HashPassword(newPassword);
            resetToken.Used = true;
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<bool> ChangePasswordAsync(string maDangNhap, string currentPassword, string newPassword)
        {
            // Find user by MaDangNhap
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.MaDangNhap == maDangNhap);
            if (user == null) return false;

            // Verify current password
            var hashed = user.MatKhau ?? string.Empty;
            if (!_passwordHashService.VerifyPassword(currentPassword, hashed))
                return false;

            // Update to new hashed password
            user.MatKhau = _passwordHashService.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Read from EmailSettings section
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

            _logger.LogInformation("Attempting to send email to {To} using SMTP host {Host}:{Port} from {From}", toEmail, smtpHost, smtpPort, fromAddress);

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

            // Ensure sender receives a copy for debugging/verification
            try
            {
                if (!string.IsNullOrWhiteSpace(smtpUser) && !mail.Bcc.Any())
                {
                    mail.Bcc.Add(smtpUser);
                }
            }
            catch { /* ignore */ }

            try
            {
                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation("Email successfully sent to {To}. BCC: {Bcc}", toEmail, smtpUser);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error when sending email to {To} via {Host}:{Port}", toEmail, smtpHost, smtpPort);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when sending email to {To}", toEmail);
                throw;
            }
        }


        public async Task<string?> GetLatestResetTokenByEmailAsync(string email)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            var tokenEntry = await _context.PasswordResetTokens
                .Where(t => t.MaNguoiDung == user.MaNguoiDung)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            return tokenEntry?.Token;
        }

        // ✅ LẤY DANH SÁCH THIẾT BỊ ĐANG ĐĂNG NHẬP
        public async Task<ActiveSessionsResponse> GetActiveSessionsAsync(int userId, int currentSessionId)
        {
            try
            {
                var now = DateTime.Now;

                // Lấy tất cả session đang hoạt động của user
                var sessions = await _context.UserSessions
                    .Where(s => s.MaNguoiDung == userId && 
                               s.IsRevoked == false &&
                               (s.ExpiresAt == null || s.ExpiresAt > now))
                    .OrderByDescending(s => s.LastAccessAt ?? s.CreatedAt)
                    .Select(s => new UserSessionDto
                    {
                        SessionId = s.Id,
                        DeviceInfo = s.DeviceInfo ?? "Unknown Device",
                        IpAddress = s.IpAddress ?? "Unknown IP",
                        CreatedAt = s.CreatedAt,
                        LastAccessAt = s.LastAccessAt,
                        ExpiresAt = s.ExpiresAt,
                        IsCurrentSession = s.Id == currentSessionId,
                        Status = "Active"
                    })
                    .ToListAsync();

                // ✅ Tìm thiết bị hiện tại
                var currentDevice = sessions.FirstOrDefault(s => s.SessionId == currentSessionId);

                _logger.LogInformation("User {UserId} has {Count} active sessions. Current session: {CurrentSessionId}", 
                    userId, sessions.Count, currentSessionId);

                return new ActiveSessionsResponse
                {
                    Success = true,
                    Message = "Lấy danh sách thiết bị thành công",
                    TotalActiveSessions = sessions.Count,
                    MaxAllowedSessions = 3,
                    CurrentDevice = currentDevice, // ✅ Thêm thông tin thiết bị hiện tại
                    Sessions = sessions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
                return new ActiveSessionsResponse
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách thiết bị",
                    TotalActiveSessions = 0,
                    MaxAllowedSessions = 3,
                    CurrentDevice = null,
                    Sessions = new List<UserSessionDto>()
                };
            }
        }

        // ✅ ĐĂNG XUẤT MỘT THIẾT BỊ CỤ THỂ
        public async Task<(bool Success, string Message)> LogoutSessionAsync(int userId, int sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.MaNguoiDung == userId);

                if (session == null)
                {
                    return (false, "Không tìm thấy phiên đăng nhập");
                }

                if (session.IsRevoked)
                {
                    return (false, "Phiên đăng nhập đã bị thu hồi trước đó");
                }

                // Thu hồi session
                session.IsRevoked = true;
                session.RevokeReason = "User logged out manually";
                
                await _context.SaveChangesAsync();

                // ✅ GỬI SIGNALR NOTIFICATION ĐỂ ĐĂNG XUẤT THIẾT BỊ REALTIME
                await _authNotificationService.NotifySessionLogoutAsync(
                    sessionId, 
                    "Thiết bị đã bị đăng xuất từ thiết bị khác");

                _logger.LogInformation("User {UserId} logged out session {SessionId} on device {DeviceInfo}", 
                    userId, sessionId, session.DeviceInfo);

                return (true, "Đăng xuất thiết bị thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out session {SessionId} for user {UserId}", sessionId, userId);
                return (false, "Lỗi khi đăng xuất thiết bị");
            }
        }

        // ✅ ĐĂNG XUẤT TẤT CẢ THIẾT BỊ KHÁC (GIỮ LẠI THIẾT BỊ HIỆN TẠI)
        public async Task<(bool Success, string Message)> LogoutOtherSessionsAsync(int userId, int currentSessionId)
        {
            try
            {
                var now = DateTime.Now;

                // Tìm tất cả session khác đang hoạt động
                var otherSessions = await _context.UserSessions
                    .Where(s => s.MaNguoiDung == userId && 
                               s.Id != currentSessionId &&
                               s.IsRevoked == false &&
                               (s.ExpiresAt == null || s.ExpiresAt > now))
                    .ToListAsync();

                if (!otherSessions.Any())
                {
                    return (true, "Không có thiết bị nào khác đang đăng nhập");
                }

                // Thu hồi tất cả session khác
                foreach (var session in otherSessions)
                {
                    session.IsRevoked = true;
                    session.RevokeReason = "Logged out by user from another device";
                }

                await _context.SaveChangesAsync();

                // ✅ GỬI SIGNALR NOTIFICATION ĐỂ ĐĂNG XUẤT TẤT CẢ THIẾT BỊ KHÁC
                await _authNotificationService.NotifyUserSessionsLogoutAsync(
                    userId, 
                    currentSessionId, 
                    "Tất cả thiết bị khác đã được đăng xuất");

                _logger.LogInformation("User {UserId} logged out {Count} other sessions from session {CurrentSessionId}", 
                    userId, otherSessions.Count, currentSessionId);

                return (true, $"Đã đăng xuất {otherSessions.Count} thiết bị khác thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out other sessions for user {UserId}", userId);
                return (false, "Lỗi khi đăng xuất các thiết bị khác");
            }
        }
    }
}