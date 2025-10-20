using HUIT_Library.DTOs.Request;

namespace HUIT_Library.Services.IServices
{
    public interface IBookingService
    {
        Task<(bool Success, string? Message)> CreateBookingRequestAsync(int userId, CreateBookingRequest request);
        Task<(bool Success, string? Message)> ExtendBookingAsync(int userId, ExtendBookingRequest request);
    }
}
