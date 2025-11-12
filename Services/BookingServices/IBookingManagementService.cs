using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;

namespace HUIT_Library.Services.BookingServices
{
    /// <summary>
    /// Interface cho quản lý trạng thái đặt phòng
    /// </summary>
    public interface IBookingManagementService
    {
        /// <summary>
        /// Tạo yêu cầu đặt phòng mới
        /// </summary>
        Task<(bool Success, string Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request);

      /// <summary>
        /// Gia hạn thời gian sử dụng phòng
        /// </summary>
     Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request);

        /// <summary>
        /// Xác nhận trả phòng
        /// </summary>
        Task<(bool Success, string? Message)> CompleteBookingAsync(int userId, int maDangKy);

        /// <summary>
        /// Hủy đặt phòng
        /// </summary>
     Task<(bool Success, string? Message)> CancelBookingAsync(int userId, int maDangKy);
    }
}