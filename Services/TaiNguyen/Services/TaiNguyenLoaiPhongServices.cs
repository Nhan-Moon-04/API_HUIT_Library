using Dapper;
using HUIT_Library.DTOs;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using HUIT_Library.Services.TaiNguyen.IServices;

namespace HUIT_Library.Services.TaiNguyen.Services
{
    public class TaiNguyenLoaiPhongServices : ITaiNguyenLoaiPhongServices
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<TaiNguyenLoaiPhongServices> _logger;

        public TaiNguyenLoaiPhongServices(HuitThuVienContext context, ILogger<TaiNguyenLoaiPhongServices> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Models.TaiNguyen>> GetAllTaiNguyenAsync()
        {
            try
            {
                _logger.LogInformation("Getting all TaiNguyen resources");
                return await _context.TaiNguyens.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all TaiNguyen resources");
                throw;
            }
        }
    }
}
