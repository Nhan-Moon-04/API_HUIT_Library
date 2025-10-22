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

    // Bot functionality
    Task<PhienChat?> CreateBotSessionAsync(int userId, CreateBotSessionRequest request);
    Task<BotResponseDto?> SendMessageToBotAsync(int userId, SendMessageRequest request);
    Task<bool> RequestStaffAsync(int userId, RequestStaffRequest request);

    // Session info
    Task<object?> GetSessionInfoAsync(int maPhienChat, int userId);

    // Debug method
    Task<string> TestBotDirectly(string message, string userId);
}
