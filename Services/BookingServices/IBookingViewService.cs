using HUIT_Library.DTOs.Response;

namespace HUIT_Library.Services.BookingServices
{
    /// <summary>
    /// Interface cho xem lịch sử và trạng thái đặt phòng
    /// </summary>
    public interface IBookingViewService
    {
        /// <summary>
        /// Lấy lịch sử đặt phòng của user
        /// </summary>
        Task<List<BookingHistoryDto>> GetBookingHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Lấy danh sách đặt phòng hiện tại
        /// </summary>
     Task<List<CurrentBookingDto>> GetCurrentBookingsAsync(int userId);

      /// <summary>
        /// Lấy chi tiết một đặt phòng
   /// </summary>
      Task<BookingHistoryDto?> GetBookingDetailsAsync(int userId, int maDangKy);

        /// <summary>
        /// Tìm kiếm lịch sử đặt phòng theo từ khóa
        /// </summary>
        Task<List<BookingHistoryDto>> SearchBookingHistoryAsync(int userId, string searchTerm, int pageNumber = 1, int pageSize = 10);
    }
}