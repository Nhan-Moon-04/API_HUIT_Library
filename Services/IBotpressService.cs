using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.Services;

public interface IBotpressService
{
    Task<string> SendMessageToBotAsync(string message, string userId);
    Task<MessageDto?> ProcessBotResponseAsync(string botResponse, int maPhienChat);
}