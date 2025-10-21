using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace HUIT_Library.Services;

public class BotpressService : IBotpressService
{
    private readonly HttpClient _httpClient;
    private readonly HuitThuVienContext _context;
    private readonly ILogger<BotpressService> _logger;
    private readonly string _botpressApiKey;
    private readonly string _botpressBaseUrl;

    public BotpressService(HttpClient httpClient, HuitThuVienContext context, IConfiguration configuration, ILogger<BotpressService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _botpressApiKey = "bp_bak_qDPXWz4ixJdg9BR7B4vBkNbUbh9NSVi17s0Z";
        _botpressBaseUrl = "https://api.botpress.cloud";

        // Configure HttpClient headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_botpressApiKey}");
        _httpClient.DefaultRequestHeaders.Add("x-bot-id", "85fc502b-d03f-4d3c-ba1b-1942a6389237");

        _logger.LogInformation("BotpressService initialized with API key: {ApiKeyPrefix}...", _botpressApiKey.Substring(0, Math.Min(10, _botpressApiKey.Length)));
    }

    public async Task<string> SendMessageToBotAsync(string message, string userId)
    {
        try
        {
            _logger.LogInformation("Sending message to Botpress API - User: {UserId}, Message: {Message}", userId, message);

            var payload = new
            {
                type = "text",
                payload = new { text = message },
                conversationId = $"user-{userId}",
                userId = userId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Botpress API request payload: {Payload}", json);

            var response = await _httpClient.PostAsync($"{_botpressBaseUrl}/v1/chat/messages", content);

            _logger.LogInformation("Botpress API response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Botpress API response content: {ResponseContent}", responseContent);

                var responseJson = JsonDocument.Parse(responseContent);

                // Extract bot response from Botpress API response
                if (responseJson.RootElement.TryGetProperty("responses", out var responses) &&
                    responses.GetArrayLength() > 0)
                {
                    var firstResponse = responses[0];
                    if (firstResponse.TryGetProperty("payload", out var responsePayload) &&
                        responsePayload.TryGetProperty("text", out var text))
                    {
                        var botResponse = text.GetString() ?? "Xin lỗi, tôi không hiểu yêu cầu của bạn.";
                        _logger.LogInformation("Bot response: {BotResponse}", botResponse);
                        return botResponse;
                    }
                }

                _logger.LogWarning("Could not parse bot response from Botpress API");
                return "Xin l?i, tôi không hi?u câu h?i c?a b?n.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Botpress API error - Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Botpress API for user {UserId} with message '{Message}'", userId, message);
        }

        return "Xin lỗi, có lỗi xảy ra với bot, Bạn có muốn liên hệ với nhân viên không?";
    }

    public async Task<MessageDto?> ProcessBotResponseAsync(string botResponse, int maPhienChat)
    {
        try
        {
            _logger.LogInformation("Processing bot response for session {SessionId}: {BotResponse}", maPhienChat, botResponse);

            // Create bot message in database
            var botMessage = new TinNhan
            {
                MaPhienChat = maPhienChat,
                MaNguoiGui = 0, // 0 indicates system/bot message
                NoiDung = botResponse,
                ThoiGianGui = DateTime.UtcNow,
                LaBot = true
            };

            _context.TinNhans.Add(botMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully saved bot message {MessageId} for session {SessionId}", botMessage.MaTinNhan, maPhienChat);

            return new MessageDto
            {
                MaTinNhan = botMessage.MaTinNhan,
                MaPhienChat = botMessage.MaPhienChat,
                MaNguoiGui = botMessage.MaNguoiGui,
                NoiDung = botMessage.NoiDung,
                ThoiGianGui = botMessage.ThoiGianGui,
                LaBot = botMessage.LaBot
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bot response for session {SessionId}: {BotResponse}", maPhienChat, botResponse);
            return null;
        }
    }
}