using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;

namespace HUIT_Library.Services.IServices
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId);
        Task<NotificationDetailsDto?> GetNotificationDetailsAsync(int userId, int notificationId);
 
    }
}
