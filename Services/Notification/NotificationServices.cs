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
using HUIT_Library.Services.IServices;

namespace HUIT_Library.Services.Notification
{
    public class NotificationServices : INotification
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<NotificationServices> _logger;

        public NotificationServices(HuitThuVienContext context, ILogger<NotificationServices> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CountNotificationAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Counting unread notifications for user {UserId}", userId);

                var count = await _context.ThongBaos
          .Where(tb => tb.MaNguoiDung == userId && tb.DaDoc == false)
           .CountAsync();

                _logger.LogInformation("User {UserId} has {Count} unread notifications", userId, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting notifications for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<ThongBao>> GetNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting notifications for user {UserId}, page {PageNumber}, size {PageSize}",
               userId, pageNumber, pageSize);

                var notifications = await _context.ThongBaos
                 .Where(tb => tb.MaNguoiDung == userId)
                      .OrderByDescending(tb => tb.NgayTao)
                           .Skip((pageNumber - 1) * pageSize)
                     .Take(pageSize)
                 .ToListAsync();

                _logger.LogInformation("Retrieved {Count} notifications for user {UserId}", notifications.Count, userId);
                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new List<ThongBao>();
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}",
          notificationId, userId);

                var notification = await _context.ThongBaos
                                  .FirstOrDefaultAsync(tb => tb.MaThongBao == notificationId && tb.MaNguoiDung == userId);

                if (notification == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found for user {UserId}",
           notificationId, userId);
                    return false;
                }

                notification.DaDoc = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully marked notification {NotificationId} as read", notificationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}",
                  notificationId, userId);
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

                var unreadNotifications = await _context.ThongBaos
              .Where(tb => tb.MaNguoiDung == userId && tb.DaDoc == false)
               .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.DaDoc = true;
                }

                var updatedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
            updatedCount, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return false;
            }
        }
    }
}