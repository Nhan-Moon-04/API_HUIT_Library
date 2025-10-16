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
using HUIT_Library.DTOs.DTO;
using HUIT_Library.Services.IServices;
using HUIT_Library.DTOs.Request;
namespace HUIT_Library.Services
{
    public class ProfileService : IProfileService
    {
        private readonly HuitThuVienContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfileService> _logger;
        public ProfileService(HuitThuVienContext context, IConfiguration configuration, ILogger<ProfileService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
        {

            string sql = "SELECT MaNguoiDung, MaDangNhap, MaVaiTro, HoTen, Email, SoDienThoai FROM NguoiDung WHERE MaNguoiDung = @UserId";
            using (var connection = _context.Database.GetDbConnection())
            {
                var userProfile = await connection.QueryFirstOrDefaultAsync<UserProfileDto>(sql, new { UserId = userId });
                return userProfile;
            }
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var user = await _context.NguoiDungs.FindAsync(request.UserId);
            if (user == null)
            {
                return false;
            }
            user.HoTen = request.FullName;
            user.Email = request.Email;
            user.SoDienThoai = request.PhoneNumber;
            _context.NguoiDungs.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}