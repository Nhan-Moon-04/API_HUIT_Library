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

            _logger.LogInformation("Creating Botpress user with payload: {Payload}", json);

            var response = await _httpClient.PostAsync(createUserUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Botpress user creation response: Status={Status}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var userResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (userResponse.TryGetProperty("key", out var keyElement))
                {
                    var userKey = keyElement.GetString();
                    _logger.LogInformation("Created/Retrieved Botpress user {UserId} with key {UserKey}", userId, userKey);
                    return userKey;
                }
                else
                {
                    _logger.LogWarning("Response does not contain 'key' field. Response: {Response}", responseContent);
                }
            }
            else
            {
                _logger.LogWarning("Failed to create/get Botpress user {UserId}. Status: {Status}, Response: {Response}",
                    userId, response.StatusCode, responseContent);
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

            _logger.LogInformation("Creating conversation with user key: {UserKey}", userKey);

            var response = await _httpClient.PostAsync(createConvUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Conversation creation response: Status={Status}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var convResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (convResponse.TryGetProperty("conversation", out var conversationElem) &&
                    conversationElem.TryGetProperty("id", out var idElement))
                {
                    var conversationId = idElement.GetString();
                    _logger.LogInformation("Created Botpress conversation {ConversationId} for user key {UserKey}", conversationId, userKey);
                    return conversationId;
                }
                else
                {
                    _logger.LogWarning("Response does not contain 'conversation.id' field. Response: {Response}", responseContent);
                }
            }
            else
            {
                _logger.LogWarning("Failed to create Botpress conversation for user key {UserKey}. Status: {Status}, Response: {Response}",
                    userKey, response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Botpress conversation for user key {UserKey}", userKey);
        }

        return null;
    }

    // Get or create reusable conversation for user
    private async Task<(string? conversationId, string? userKey)> GetOrCreateConversationAsync(string userId)
    {
        try
        {
            // First, get or create user key
            var userKey = await CreateOrGetBotpressUserAsync(userId);
            if (string.IsNullOrEmpty(userKey))
                return (null, null);

            // Parse userId to int for database query
            if (!int.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("Cannot parse userId '{UserId}' to int", userId);
                return (null, null);
            }

            // Check for existing active conversation
            var existingConversation = await _context.BotConversations
                .Where(c => c.UserId == parsedUserId && c.IsActive)
                .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingConversation != null)
            {
                // Update last used time to Vietnam timezone
                existingConversation.LastUsedAt = GetVietnamTime();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reusing existing conversation {ConversationId} for user {UserId}",
                    existingConversation.ConversationId, userId);

                return (existingConversation.ConversationId, existingConversation.UserKey);
            }

            // Create new conversation
            var newConversationId = await CreateBotpressConversationAsync(userKey);
            if (string.IsNullOrEmpty(newConversationId))
                return (null, null);

            // Save to database with Vietnam timezone
            var botConversation = new BotConversation
            {
                UserId = parsedUserId,
                UserKey = userKey,
                ConversationId = newConversationId,
                CreatedAt = GetVietnamTime(),
                LastUsedAt = GetVietnamTime(),
                IsActive = true
            };

            _context.BotConversations.Add(botConversation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created and saved new conversation {ConversationId} for user {UserId}",
                newConversationId, userId);

            return (newConversationId, userKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting/creating conversation for user {UserId}", userId);
            return (null, null);
        }
    }

    // Helper method to get Vietnam timezone
    private DateTime GetVietnamTime()
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
    }

    // Poll for bot response using GET method like in your example
    private async Task<string?> PollForBotResponseAsync(string conversationId, string userKey, string currentUserId, int maxRetries = 6)
    {
        try
        {
            var getMessagesUrl = $"{_botpressBaseUrl}/conversations/{conversationId}/messages";
            DateTime pollStartTime = DateTime.UtcNow;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                _httpClient.DefaultRequestHeaders.Clear();
                
                // Set headers theo format API của bạn
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {userKey}");
                _httpClient.DefaultRequestHeaders.Add("x-user-key", userKey);

                _logger.LogInformation("Polling attempt {Attempt}/{MaxRetries} for conversation {ConversationId} with userKey {UserKey}", 
                    attempt + 1, maxRetries, conversationId, userKey);

                var response = await _httpClient.GetAsync(getMessagesUrl);
                var messagesContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Polling response: Status={Status}, Content={Content}",
                    response.StatusCode, messagesContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var messagesJson = JsonSerializer.Deserialize<JsonElement>(messagesContent);
                        
                        if (!messagesJson.TryGetProperty("messages", out var messagesArray))
                        {
                            _logger.LogWarning("No 'messages' property found in response on attempt {Attempt}", attempt + 1);
                            continue;
                        }

                        if (messagesArray.ValueKind == JsonValueKind.Array)
                        {
                            var messages = messagesArray.EnumerateArray().ToArray();
                            _logger.LogInformation("Found {Count} messages in conversation", messages.Length);

                            // Tìm bot message mới nhất (không phải từ current user)
                            var botMessages = messages
                                .Where(m => {
                                    if (m.TryGetProperty("userId", out var userIdProp) &&
                                        m.TryGetProperty("createdAt", out var createdAtProp))
                                    {
                                        var messageUserId = userIdProp.GetString();
                                        var createdAtStr = createdAtProp.GetString();
                                        
                                        // Check if it's NOT from current user
                                        bool isNotFromUser = !string.IsNullOrEmpty(messageUserId) && !messageUserId.Contains(currentUserId);
                                        
                                        // Check if message is recent (created after we started polling)
                                        bool isRecentMessage = true;
                                        if (DateTime.TryParse(createdAtStr, out var createdAt))
                                        {
                                            isRecentMessage = createdAt >= pollStartTime.AddSeconds(-30); // 30 second buffer
                                        }
                                        
                                        return isNotFromUser && isRecentMessage;
                                    }
                                    return false;
                                })
                                .OrderByDescending(m => {
                                    if (m.TryGetProperty("createdAt", out var createdAt))
                                        return createdAt.GetString();
                                    return "";
                                })
                                .ToArray();

                            _logger.LogInformation("Found {Count} recent bot messages", botMessages.Length);

                            if (botMessages.Length > 0)
                            {
                                var latestBotMessage = botMessages.FirstOrDefault();
                                if (latestBotMessage.ValueKind != JsonValueKind.Undefined &&
                                    latestBotMessage.TryGetProperty("payload", out var payload) &&
                                    payload.TryGetProperty("text", out var text))
                                {
                                    var botResponseText = text.GetString();
                                    if (!string.IsNullOrEmpty(botResponseText))
                                    {
                                        _logger.LogInformation("Found valid bot response on attempt {Attempt}: {Response}", 
                                            attempt + 1, botResponseText);
                                        
                                        // Save bot message to database
                                        try
                                        {
                                            await SaveBotMessageToDatabase(currentUserId, botResponseText);
                                        }
                                        catch (Exception saveEx)
                                        {
                                            _logger.LogError(saveEx, "Failed to save bot message to database");
                                        }
                                        
                                        return botResponseText;
                                    }
                                }
                            }

                            _logger.LogInformation("No new bot messages found on attempt {Attempt}, retrying...", attempt + 1);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON parsing error on attempt {Attempt}: {Content}", attempt + 1, messagesContent);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get messages on attempt {Attempt}. Status: {Status}, Response: {Response}", 
                        attempt + 1, response.StatusCode, messagesContent);
                }

                // Wait before retry: 2s, 4s, 6s, 8s, 10s, 12s
                var delayMs = (attempt + 1) * 2000;
                _logger.LogInformation("Waiting {DelayMs}ms before next attempt...", delayMs);
                await Task.Delay(delayMs);
            }

            _logger.LogWarning("No bot response found after {MaxRetries} attempts for conversation {ConversationId}", maxRetries, conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for bot response in conversation {ConversationId}", conversationId);
            return null;
        }
    }

    // Helper method to save bot message directly to database
    private async Task SaveBotMessageToDatabase(string userId, string botResponseText)
    {
        try
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("Cannot parse userId '{UserId}' to int when saving bot message", userId);
                return;
            }

            // Find or create bot session for this user
            var session = await _context.PhienChats
                .Where(p => p.MaNguoiDung == parsedUserId && p.CoBot == true)
                .OrderByDescending(p => p.ThoiGianBatDau)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                _logger.LogWarning("No bot session found for user {UserId}, creating new one", userId);
                session = new PhienChat
                {
                    MaNguoiDung = parsedUserId,
                    CoBot = true,
                    ThoiGianBatDau = GetVietnamTime()
                };
                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();
            }

            // Save bot message
            var botMessage = new TinNhan
            {
                MaPhienChat = session.MaPhienChat,
                MaNguoiGui = 0, // System/Bot user ID
                NoiDung = botResponseText,
                ThoiGianGui = GetVietnamTime(),
                LaBot = true
            };

            _context.TinNhans.Add(botMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Successfully saved bot message to database for user {UserId}: {Text}", userId, botResponseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error saving bot message to database for user {UserId}", userId);
        }
    }

    // Send message to Botpress and get conversation messages
    public async Task<string> SendMessageToBotAsync(string message, string userId)
    {
        try
        {
            _logger.LogInformation("Sending message to Botpress for user {UserId}: {Message}", userId, message);

            // Step 1: Get or create reusable conversation
            var (conversationId, userKey) = await GetOrCreateConversationAsync(userId);
            if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(userKey))
            {
                return "Lỗi: Không thể tạo hoặc lấy cuộc hội thoại với bot";
            }

            _logger.LogInformation("Using conversation {ConversationId} with userKey {UserKey}", conversationId, userKey);

            // Step 2: Send message
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
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {userKey}");
            _httpClient.DefaultRequestHeaders.Add("x-user-key", userKey);

            _logger.LogInformation("Sending message with payload: {Payload}", json);

            var sendResponse = await _httpClient.PostAsync(sendMessageUrl, content);
            var sendResponseContent = await sendResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("Send message response: Status={Status}, Content={Content}",
                sendResponse.StatusCode, sendResponseContent);

            if (!sendResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send message to Botpress. Status: {Status}, Response: {Response}",
                    sendResponse.StatusCode, sendResponseContent);
                return "Lỗi: Không thể gửi tin nhắn đến bot";
            }

            // Step 3: Poll for bot response (bot message will be saved automatically in polling)
            _logger.LogInformation("Starting to poll for bot response...");
            var botResponse = await PollForBotResponseAsync(conversationId, userKey, userId);

            if (!string.IsNullOrEmpty(botResponse))
            {
                _logger.LogInformation("Successfully received and saved bot response: {Response}", botResponse);
                return botResponse;
            }

            _logger.LogWarning("Bot polling completed but no response received");
            return "Xin lỗi, bot đang bận xử lý. Vui lòng thử lại sau ít phút.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Botpress for user {UserId}", userId);
            return $"Lỗi khi gửi tin nhắn đến bot: {ex.Message}";
        }
    }

    // Process bot response and save to DB as a bot message, returning MessageDto
    // NOTE: This method is now mainly used for manual processing, as polling automatically saves bot messages
    public async Task<MessageDto?> ProcessBotResponseAsync(string botResponse, int maPhienChat)
    {
        try
        {
            // Check if this bot response already exists in the database to avoid duplicates
            var existingMessage = await _context.TinNhans
                .Where(t => t.MaPhienChat == maPhienChat && t.LaBot == true && t.NoiDung == botResponse)
                .OrderByDescending(t => t.ThoiGianGui)
                .FirstOrDefaultAsync();

            if (existingMessage != null)
            {
                _logger.LogInformation("Bot response already exists in database, returning existing message: {MessageId}", existingMessage.MaTinNhan);
                return new MessageDto
                {
                    MaTinNhan = existingMessage.MaTinNhan,
                    MaPhienChat = existingMessage.MaPhienChat,
                    MaNguoiGui = existingMessage.MaNguoiGui,
                    NoiDung = existingMessage.NoiDung,
                    ThoiGianGui = existingMessage.ThoiGianGui,
                    LaBot = existingMessage.LaBot
                };
            }

            var session = await _context.PhienChats.FindAsync(maPhienChat);
            if (session == null) return null;

            var botMessage = new TinNhan
            {
                MaPhienChat = maPhienChat,
                MaNguoiGui = 0, // System/Bot user ID
                NoiDung = botResponse,
                ThoiGianGui = GetVietnamTime(),
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

    // Save bot message from webhook to DB
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
                    ThoiGianBatDau = GetVietnamTime()
                };
                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();
            }

            var message = new TinNhan
            {
                MaPhienChat = session.MaPhienChat,
                MaNguoiGui = 0,
                NoiDung = text,
                ThoiGianGui = GetVietnamTime(),
                LaBot = true
            };

            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Saved bot message from Botpress for user {UserId}: {Text}", userId, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error saving bot message from Botpress for user {UserId}", userId);
        }
    }

    // Handle webhook data from Botpress
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
            _logger.LogError(ex, "Error handling webhook payload from Botpress");
        }
    }
}