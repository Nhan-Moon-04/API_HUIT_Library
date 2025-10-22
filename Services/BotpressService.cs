using HUIT_Library.DTOs.DTO;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HUIT_Library.Services;

public class BotpressService : IBotpressService
{
    private readonly HuitThuVienContext _context;
    private readonly ILogger<BotpressService> _logger;

    public BotpressService(HuitThuVienContext context, ILogger<BotpressService> logger)
    {
        _context = context;
        _logger = logger;
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

    // Send message to bot (stub/simulated implementation)
    public async Task<string> SendMessageToBotAsync(string message, string userId)
    {
        // For now, simulate a bot response. Replace with real API call when available.
        await Task.Yield();
        _logger.LogInformation("Sending message to bot for user {UserId}: {Message}", userId, message);
        var simulatedResponse = $"Bot trả lời: Tôi đã nhận được - {message}";
        return simulatedResponse;
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
                MaNguoiGui = 0,
                NoiDung = botResponse,
                ThoiGianGui = DateTime.UtcNow,
                LaBot = true
            };

            _context.TinNhans.Add(botMessage);
            await _context.SaveChangesAsync();

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
            _logger.LogError(ex, "Lỗi khi xử lý phản hồi từ Botpress");
            return null;
        }
    }
}
