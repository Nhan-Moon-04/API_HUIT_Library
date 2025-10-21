// Add interface for chat service
using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;

namespace HUIT_Library.Services.IServices;

public interface IChatService
{
    Task<TinNhan?> SendMessageAsync(int userId, SendMessageRequest request);
    Task<IEnumerable<MessageDto>> GetMessagesAsync(int maPhienChat);
    Task<PhienChat?> CreateSessionAsync(int userId);
}
