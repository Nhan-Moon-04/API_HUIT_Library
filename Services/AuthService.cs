using HUIT_Library.DTOs;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HUIT_Library.Services
{
    public class AuthService : IAuthService
    {
        private readonly HuitThuVienContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHashService _passwordHashService;

        public AuthService(HuitThuVienContext context, IConfiguration configuration, IPasswordHashService passwordHashService)
        {
            _context = context;
            _configuration = configuration;
            _passwordHashService = passwordHashService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Tìm user bằng MaNhanVien hoặc MaSinhVien (sử dụng MaDangNhap để tìm cả hai)
                var user = await _context.NguoiDungs
                    .Include(u => u.MaVaiTroNavigation)
                    .FirstOrDefaultAsync(u => 
                        (u.MaNhanVien == request.MaDangNhap || u.MaSinhVien == request.MaDangNhap) 
                        && u.TrangThai == true);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Verify password
                if (!_passwordHashService.VerifyPassword(request.MatKhau, user.MatKhau ?? ""))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Chỉ cho phép GIANG_VIEN và SINH_VIEN đăng nhập qua API này
                if (user.MaVaiTroNavigation.MaCode != "GIANG_VIEN" && user.MaVaiTroNavigation.MaCode != "SINH_VIEN")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập hệ thống này!"
                    };
                }

                var token = GenerateJwtToken(user, user.MaVaiTroNavigation.MaCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = token,
                    User = new UserInfo
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        MaCode = user.MaCode,
                        MaSinhVien = user.MaSinhVien,
                        MaNhanVien = user.MaNhanVien,
                        HoTen = user.HoTen,
                        Email = user.Email,
                        SoDienThoai = user.SoDienThoai,
                        VaiTro = user.MaVaiTroNavigation.MaCode,
                        Lop = user.Lop,
                        KhoaHoc = user.KhoaHoc,
                        NgaySinh = user.NgaySinh
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
                // Tìm user bằng MaNhanVien (chỉ admin và nhân viên có MaNhanVien)
                var user = await _context.NguoiDungs
                    .Include(u => u.MaVaiTroNavigation)
                    .FirstOrDefaultAsync(u => 
                        u.MaNhanVien == request.MaDangNhap 
                        && u.TrangThai == true);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Verify password
                if (!_passwordHashService.VerifyPassword(request.MatKhau, user.MatKhau ?? ""))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Thông tin đăng nhập không chính xác!"
                    };
                }

                // Chỉ cho phép QUAN_TRI và NHAN_VIEN đăng nhập qua API này
                if (user.MaVaiTroNavigation.MaCode != "QUAN_TRI" && user.MaVaiTroNavigation.MaCode != "NHAN_VIEN")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền truy cập hệ thống quản lý!"
                    };
                }

                var token = GenerateJwtToken(user, user.MaVaiTroNavigation.MaCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Token = token,
                    User = new UserInfo
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        MaCode = user.MaCode,
                        MaSinhVien = user.MaSinhVien,
                        MaNhanVien = user.MaNhanVien,
                        HoTen = user.HoTen,
                        Email = user.Email,
                        SoDienThoai = user.SoDienThoai,
                        VaiTro = user.MaVaiTroNavigation.MaCode,
                        Lop = user.Lop,
                        KhoaHoc = user.KhoaHoc,
                        NgaySinh = user.NgaySinh
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

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"DEBUG - VaiTro: {request.VaiTro}");
                Console.WriteLine($"DEBUG - MaSinhVien: {request.MaSinhVien ?? "NULL"}");
                Console.WriteLine($"DEBUG - MaNhanVien: {request.MaNhanVien ?? "NULL"}");

                // Kiểm tra email đã tồn tại
                var existingUserByEmail = await _context.NguoiDungs
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUserByEmail != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email đã được sử dụng!"
                    };
                }

                // Kiểm tra mã sinh viên hoặc mã nhân viên đã tồn tại
                if (request.VaiTro == "SINH_VIEN")
                {
                    var existingSinhVien = await _context.NguoiDungs
                        .FirstOrDefaultAsync(u => u.MaSinhVien == request.MaSinhVien);
                    if (existingSinhVien != null)
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Mã sinh viên đã được sử dụng!"
                        };
                    }
                }
                else if (request.VaiTro == "GIANG_VIEN")
                {
                    var existingGiangVien = await _context.NguoiDungs
                        .FirstOrDefaultAsync(u => u.MaNhanVien == request.MaNhanVien);
                    if (existingGiangVien != null)
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Mã nhân viên đã được sử dụng!"
                        };
                    }
                }

                // Lấy vai trò
                var vaiTro = await _context.VaiTroNguoiDungs
                    .FirstOrDefaultAsync(v => v.MaCode == request.VaiTro && v.TrangThai == true);
                if (vaiTro == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Vai trò không hợp lệ!"
                    };
                }

                // Tạo mã code cho user
                var maCode = request.VaiTro == "SINH_VIEN" ? request.MaSinhVien : request.MaNhanVien;
                Console.WriteLine($"DEBUG - MaCode: {maCode ?? "NULL"}");

                // Hash password
                var hashedPassword = _passwordHashService.HashPassword(request.MatKhau);

                // Tạo user mới
                var newUser = new NguoiDung
                {
                    MaCode = maCode!,
                    HoTen = request.HoTen,
                    Email = request.Email,
                    SoDienThoai = request.SoDienThoai,
                    MatKhau = hashedPassword,
                    MaVaiTro = vaiTro.MaVaiTro,
                    NgaySinh = request.NgaySinh,
                    TrangThai = true
                };

                // Gán giá trị MaSinhVien hoặc MaNhanVien dựa trên vai trò
                if (request.VaiTro == "SINH_VIEN")
                {
                    newUser.MaSinhVien = request.MaSinhVien;
                    newUser.MaNhanVien = null;
                    newUser.Lop = request.Lop;
                    newUser.KhoaHoc = request.KhoaHoc;
                }
                else if (request.VaiTro == "GIANG_VIEN")
                {
                    newUser.MaSinhVien = null;
                    newUser.MaNhanVien = request.MaNhanVien;
                    newUser.Lop = null;
                    newUser.KhoaHoc = null;
                }

                Console.WriteLine($"DEBUG - Before Save - MaSinhVien: {newUser.MaSinhVien ?? "NULL"}");
                Console.WriteLine($"DEBUG - Before Save - MaNhanVien: {newUser.MaNhanVien ?? "NULL"}");
                Console.WriteLine($"DEBUG - Before Save - MaCode: {newUser.MaCode ?? "NULL"}");
                Console.WriteLine($"DEBUG - Before Save - VaiTro: {request.VaiTro}");

                _context.NguoiDungs.Add(newUser);
                await _context.SaveChangesAsync();

                Console.WriteLine($"DEBUG - After Save - ID: {newUser.MaNguoiDung}");

                // Load lại user với thông tin vai trò
                var userWithRole = await _context.NguoiDungs
                    .Include(u => u.MaVaiTroNavigation)
                    .FirstOrDefaultAsync(u => u.MaNguoiDung == newUser.MaNguoiDung);


                var token = GenerateJwtToken(userWithRole!, vaiTro.MaCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Đăng ký thành công!",
                    Token = token,
                    User = new UserInfo
                    {
                        MaNguoiDung = userWithRole!.MaNguoiDung,
                        MaCode = userWithRole.MaCode,
                        MaSinhVien = userWithRole.MaSinhVien,
                        MaNhanVien = userWithRole.MaNhanVien,
                        HoTen = userWithRole.HoTen,
                        Email = userWithRole.Email,
                        SoDienThoai = userWithRole.SoDienThoai,
                        VaiTro = vaiTro.MaCode,
                        Lop = userWithRole.Lop,
                        KhoaHoc = userWithRole.KhoaHoc,
                        NgaySinh = userWithRole.NgaySinh
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG - Exception: {ex.Message}");
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
                new("MaCode", user.MaCode),
                new("VaiTro", role)
            };

            if (!string.IsNullOrEmpty(user.MaSinhVien))
                claims.Add(new Claim("MaSinhVien", user.MaSinhVien));
            
            if (!string.IsNullOrEmpty(user.MaNhanVien))
                claims.Add(new Claim("MaNhanVien", user.MaNhanVien));

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
    }
}