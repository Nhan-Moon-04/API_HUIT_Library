using HUIT_Library.DTOs.DTO;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace HUIT_Library.Services;

public class BotpressService : IBotpressService
{
    private readonly HuitThuVienContext _context;
    private readonly ILogger<BotpressService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _botpressBaseUrl;

    public BotpressService(HuitThuVienContext context, ILogger<BotpressService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _botpressBaseUrl = configuration["Botpress:BaseUrl"] ?? "https://webchat.botpress.cloud/9dc9e0f4-9fad-4dc5-b5bc-aea77f87abc5";
    }

    // Create or get existing user in Botpress
    private async Task<string?> CreateOrGetBotpressUserAsync(string userId)
    {
        try
        {
            var createUserUrl = $"{_botpressBaseUrl}/users";
            var userPayload = new { id = userId };
            var json = JsonSerializer.Serialize(userPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(createUserUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (userResponse.TryGetProperty("key", out var keyElement))
                {
                    var userKey = keyElement.GetString();
                    _logger.LogInformation("Created/Retrieved Botpress user {UserId} with key {UserKey}", userId, userKey);
                    return userKey;
                }
            }
            else
            {
                _logger.LogWarning("Failed to create/get Botpress user {UserId}. Status: {Status}", userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/getting Botpress user {UserId}", userId);
        }
        
        return null;
    }

    // Create conversation in Botpress
    private async Task<string?> CreateBotpressConversationAsync(string userKey)
    {
        try
        {
            var createConvUrl = $"{_botpressBaseUrl}/conversations";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-user-key", userKey);
            
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(createConvUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var convResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (convResponse.TryGetProperty("id", out var idElement))
                {
                    var conversationId = idElement.GetString();
                    _logger.LogInformation("Created Botpress conversation {ConversationId} for user key {UserKey}", conversationId, userKey);
                    return conversationId;
                }
            }
            else
            {
                _logger.LogWarning("Failed to create Botpress conversation for user key {UserKey}. Status: {Status}", userKey, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Botpress conversation for user key {UserKey}", userKey);
        }
        
        return null;
    }

    // Send message to Botpress and get conversation messages
    public async Task<string> SendMessageToBotAsync(string message, string userId)
    {
        try
        {
            _logger.LogInformation("Sending message to Botpress for user {UserId}: {Message}", userId, message);

            // Step 1: Create/get user
            var userKey = await CreateOrGetBotpressUserAsync(userId);
            if (string.IsNullOrEmpty(userKey))
            {
                return "Lỗi: Không thể tạo người dùng trong Botpress";
            }

            // Step 2: Create conversation
            var conversationId = await CreateBotpressConversationAsync(userKey);
            if (string.IsNullOrEmpty(conversationId))
            {
                return "Lỗi: Không thể tạo cuộc hội thoại trong Botpress";
            }

            // Step 3: Send message
            var sendMessageUrl = $"{_botpressBaseUrl}/messages";
            var messagePayload = new
            {
                conversationId = conversationId,
                payload = new
                {
                    type = "text",
                    text = message
                }
            };

            var json = JsonSerializer.Serialize(messagePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-user-key", userKey);

            var sendResponse = await _httpClient.PostAsync(sendMessageUrl, content);
            
            if (!sendResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send message to Botpress. Status: {Status}", sendResponse.StatusCode);
                return "Lỗi: Không thể gửi tin nhắn đến bot";
            }

            // Step 4: Wait a bit for bot to process and respond
            await Task.Delay(1500);

            // Step 5: Get conversation messages to retrieve bot response
            var getMessagesUrl = $"{_botpressBaseUrl}/conversations/{conversationId}/messages";
            
            var getResponse = await _httpClient.GetAsync(getMessagesUrl);
            
            if (getResponse.IsSuccessStatusCode)
            {
                var messagesContent = await getResponse.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<JsonElement[]>(messagesContent);
                
                // Find the latest outgoing message (bot response)
                var botMessage = messages
                    .Where(m => m.TryGetProperty("direction", out var dir) && dir.GetString() == "outgoing")
                    .LastOrDefault();
                
                if (botMessage.ValueKind != JsonValueKind.Undefined)
                {
                    if (botMessage.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("text", out var text))
                    {
                        var botResponse = text.GetString() ?? "Bot không có phản hồi";
                        _logger.LogInformation("Received bot response for user {UserId}: {Response}", userId, botResponse);
                        return botResponse;
                    }
                }
            }

            return "Bot đã nhận tin nhắn nhưng chưa có phản hồi";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Botpress for user {UserId}", userId);
            return $"Lỗi khi gửi tin nhắn đến bot: {ex.Message}";
        }
    }

    // Process bot response and save to DB as a bot message, returning MessageDto
    public async Task<MessageDto?> ProcessBotResponseAsync(string botResponse, int maPhienChat)
    {
        try
        {
            var session = await _context.PhienChats.FindAsync(maPhienChat);
            if (session == null) return null;

            var botMessage = new TinNhan
            {
                MaPhienChat = maPhienChat,
                MaNguoiGui = 0, // System/Bot user ID
                NoiDung = botResponse,
                ThoiGianGui = DateTime.UtcNow,
                LaBot = true
            };

            _context.TinNhans.Add(botMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved bot response message for session {SessionId}: {Response}", maPhienChat, botResponse);

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
            _logger.LogError(ex, "Error processing bot response for session {SessionId}", maPhienChat);
            return null;
        }
    }

    // ✅ Lưu tin nhắn từ webhook vào DB
    public async Task SaveBotMessageAsync(string userId, string text)
    {
        try
        {
            var session = await _context.PhienChats
                .Where(p => p.MaNguoiDung.ToString() == userId && p.CoBot == true)
                .OrderByDescending(p => p.ThoiGianBatDau)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                // Try to parse userId to int; if fails, skip creating session
                if (!int.TryParse(userId, out var parsedUserId))
                {
                    _logger.LogWarning("Cannot parse userId '{UserId}' to int when saving bot message", userId);
                    return;
                }

                session = new PhienChat
                {
                    MaNguoiDung = parsedUserId,
                    CoBot = true,
                    ThoiGianBatDau = DateTime.UtcNow
                };
                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();
            }

            var message = new TinNhan
            {
                MaPhienChat = session.MaPhienChat,
                MaNguoiGui = 0,
                NoiDung = text,
                ThoiGianGui = DateTime.UtcNow,
                LaBot = true
            };

            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Đã lưu tin nhắn từ Botpress cho user {UserId}: {Text}", userId, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi lưu tin nhắn từ Botpress cho user {UserId}", userId);
        }
    }

    // ✅ Nhận dữ liệu từ webhook (controller gọi tới)
    public async Task HandleWebhookAsync(JsonElement body)
    {
        try
        {
            string userId = "unknown";
            string text = string.Empty;

            // Safely extract user.id
            if (body.TryGetProperty("user", out JsonElement userElem) && userElem.ValueKind != JsonValueKind.Null)
            {
                if (userElem.TryGetProperty("id", out JsonElement idElem))
                {
                    if (idElem.ValueKind == JsonValueKind.String)
                        userId = idElem.GetString() ?? "unknown";
                    else
                        userId = idElem.GetRawText();
                }
            }

            // Safely extract payload.text
            if (body.TryGetProperty("payload", out JsonElement payloadElem) && payloadElem.ValueKind != JsonValueKind.Null)
            {
                if (payloadElem.TryGetProperty("text", out JsonElement textElem))
                {
                    if (textElem.ValueKind == JsonValueKind.String)
                        text = textElem.GetString() ?? string.Empty;
                    else
                        text = textElem.GetRawText();
                }
            }

            if (!string.IsNullOrEmpty(text))
                await SaveBotMessageAsync(userId, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý webhook payload từ Botpress");
        }
    }
}
