using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.Services.IServices
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId);
        Task<NotificationDetailsDto?> GetNotificationDetailsAsync(int userId, int notificationId);
    }
}
