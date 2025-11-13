using HUIT_Library.Models;

namespace HUIT_Library.Services.IServices
{
    public interface INotification
    {
        /// <summary>
        /// Đếm số thông báo chưa đọc của user
        /// </summary>
        Task<int> CountNotificationAsync(int userId);

        /// <summary>
        /// Lấy danh sách thông báo của user với phân trang
        /// </summary>
        Task<List<ThongBao>> GetNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Đánh dấu tất cả thông báo của user là đã đọc
        /// </summary>
        Task<bool> MarkAllAsReadAsync(int userId);

        
    }
}
