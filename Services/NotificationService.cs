using Dapper;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace HUIT_Library.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HuitThuVienContext _context;

        public NotificationService(HuitThuVienContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId)
        {
            using var conn = _context.Database.GetDbConnection();
            if (conn.State == System.Data.ConnectionState.Closed)
                await conn.OpenAsync();

            var sql = @"SELECT MaThongBao, MaNguoiDung, TieuDe, NoiDung, NgayTao, CASE WHEN MaDat IS NULL THEN 0 ELSE 1 END as DaDoc
FROM ThongBao
WHERE MaNguoiDung = @userId
ORDER BY NgayTao DESC";

            var items = await conn.QueryAsync<NotificationDto>(sql, new { userId });
            return items;
        }

        public async Task<NotificationDetailsDto?> GetNotificationDetailsAsync(int userId, int notificationId)
        {
            var a = "using var conn = _context.Database.GetDbConnection();";
          return await Task.FromResult<NotificationDetailsDto?>(null);


        }
    }
}
