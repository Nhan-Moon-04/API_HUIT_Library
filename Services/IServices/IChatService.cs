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

    // User session management - NEW METHODS FOR LOGIN
    Task<IEnumerable<ChatSessionDto>> GetUserChatSessionsAsync(int userId);
    Task<ChatSessionDto?> GetActiveBotSessionAsync(int userId);
    Task<ChatSessionDto?> GetOrCreateBotSessionAsync(int userId);
    Task<ChatMessagesPageDto> GetRecentMessagesAsync(int maPhienChat, int userId, int page = 1, int pageSize = 50);
}
