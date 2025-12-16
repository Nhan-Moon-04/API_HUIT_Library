using Dapper;
using HUIT_Library.DTOs;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace HUIT_Library.Services
{
    public class AuthService : IAuthService
    {
        private readonly HuitThuVienContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(HuitThuVienContext context, IConfiguration configuration, IPasswordHashService passwordHashService, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _passwordHashService = passwordHashService;
            _logger = logger;
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

                var token = ((IAuthService)this).GenerateJwtToken(user, roleCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = token,
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

                // Verify password
                //if (!_passwordHashService.VerifyPassword(request.MatKhau, user.MatKhau ?? ""))
                //{
                //    return new LoginResponse
                //    {
                //        Success = false,
                //        Message = "Thông tin đăng nhập không chính xác!"
                //    };
                //}

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

                var token = ((IAuthService)this).GenerateJwtToken(user, roleCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = token,
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
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        public string GenerateJwtToken(NguoiDung user, string role)
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
                new("VaiTro", role)
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
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "HUIT_Library",
                Audience = _configuration["Jwt:Audience"] ?? "HUIT_Library_Users"
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
    }
}