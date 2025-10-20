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

            var sql = @"SELECT MaThongBao, MaNguoiDung, TieuDe, NoiDung, NgayTao AS ThoiGian, CASE WHEN ISNULL(DaDoc, 0) = 1 THEN 1 ELSE 0 END as DaDoc
FROM ThongBao
WHERE MaNguoiDung = @userId
ORDER BY NgayTao DESC";

            var items = await conn.QueryAsync<NotificationDto>(sql, new { userId });
            return items;
        }

        public async Task<NotificationDetailsDto?> GetNotificationDetailsAsync(int userId, int notificationId)
        {
            // Ensure the notification belongs to the user
            var notification = await _context.ThongBaos.FirstOrDefaultAsync(t => t.MaThongBao == notificationId && t.MaNguoiDung == userId);
            if (notification == null)
                return null;

            // Map to DTO
            var dto = new NotificationDetailsDto
            {
                MaThongBao = notification.MaThongBao,
                TieuDe = notification.TieuDe,
                NoiDung = notification.NoiDung,
                ThoiGian = notification.NgayTao ?? DateTime.MinValue,
                DaDoc = notification.DaDoc == true,
                GhiChu = null
            };

            // If not yet marked as read, update DaDoc = true
            if (notification.DaDoc != true)
            {
                notification.DaDoc = true;
                try
                {
                    await _context.SaveChangesAsync();
                    dto.DaDoc = true;
                }
                catch
                {
                    // ignore save errors - still return data
                }
            }

            return dto;
        }
    }
}
