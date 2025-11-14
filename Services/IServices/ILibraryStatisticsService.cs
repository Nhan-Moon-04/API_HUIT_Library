using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.Services.IServices
{
    /// <summary>
  /// Interface cho d?ch v? th?ng kê web th? vi?n
 /// </summary>
    public interface ILibraryStatisticsService
    {
    /// <summary>
        /// L?y th?ng kê t?ng quan c?a web th? vi?n
  /// </summary>
        Task<LibraryStatisticsDto> GetLibraryStatisticsAsync();

        /// <summary>
     /// Ghi nh?n l??t truy c?p (g?i khi user vào trang)
        /// </summary>
     Task RecordVisitAsync(int? userId = null, string? ipAddress = null);

     /// <summary>
        /// C?p nh?t tr?ng thái online c?a user
    /// </summary>
    Task UpdateUserOnlineStatusAsync(int userId, bool isOnline);
    }
}