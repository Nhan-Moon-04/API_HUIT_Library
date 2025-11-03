using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;

namespace HUIT_Library.Services.IServices
{
    public interface IBookingService
    {
        Task<(bool Success, string? Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request);
        Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request);
        Task<(bool Success, string? Message)> CompleteBookingAsync(int userId, int maDangKy);
        Task<List<BookingHistoryDto>> GetBookingHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10);
        Task<List<CurrentBookingDto>> GetCurrentBookingsAsync(int userId);
    }
}
