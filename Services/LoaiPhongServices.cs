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
    public class LoaiPhongServices : ILoaiPhongServices
    {
        private readonly ILoaiPhongServices _loaiPhong;

        private readonly HuitThuVienContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfileService> _logger;
        public LoaiPhongServices(HuitThuVienContext context, IConfiguration configuration, ILogger<ProfileService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<LoaiPhong>> GetAllLoaiPhong()
        {
            var loaiPhongs = await _context.LoaiPhongs.ToListAsync();
            return loaiPhongs;
        }

        public async Task<LoaiPhong> GetLoaiPhongById(int id)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            return loaiPhong;
        }
    }
}