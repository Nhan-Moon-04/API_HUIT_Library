using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HUIT_Library.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IBotpressService _botpressService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, IBotpressService botpressService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _botpressService = botpressService;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("user/check")]
    public async Task<IActionResult> CheckCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Ok(new
            {
                success = false,
                message = "Cannot parse user ID from token",
                userIdClaim = userIdClaim,
                userName = userNameClaim,
                email = emailClaim
            });
        }

        return Ok(new
        {
            success = true,
            userId = userId,
            userName = userNameClaim,
            email = emailClaim,
            message = "User ID parsed successfully from token"
        });
    }

    [Authorize]
    [HttpGet("session/{maPhienChat}/info")]
    public async Task<IActionResult> GetSessionInfo(int maPhienChat)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var sessionInfo = await _chatService.GetSessionInfoAsync(maPhienChat, userId);
            if (sessionInfo == null)
                return BadRequest(new { message = "Phiên chat không t?n t?i ho?c b?n không có quy?n truy c?p" });

            return Ok(sessionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info for session {SessionId}", maPhienChat);
            return StatusCode(500, new { message = "Lỗi hệ thống" });
        }
    }

    [Authorize]
    [HttpPost("session/create")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatRequest? request = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Failed to parse user ID from token: {UserIdClaim}", userIdClaim);
            return Unauthorized();
        }

        _logger.LogInformation("Creating chat session for user ID: {UserId}", userId);

        try
        {
            var session = await _chatService.CreateSessionAsync(userId);
            if (session == null)
            {
                _logger.LogWarning("Failed to create session for user ID: {UserId}", userId);
                return BadRequest(new { message = "Không th? t?o phiên chat" });
            }

            _logger.LogInformation("Successfully created chat session {SessionId} for user {UserId}", session.MaPhienChat, userId);
            return Ok(new
            {
                maPhienChat = session.MaPhienChat,
                coBot = session.CoBot,
                message = "Phiên chat thu?ng dã du?c t?o thành công"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session for user {UserId}", userId);
            return StatusCode(500, new { message = "L?i h? th?ng khi t?o phiên chat" });
        }
    }

    [Authorize]
    [HttpPost("bot-session/create")]
    public async Task<IActionResult> CreateBotSession([FromBody] CreateBotSessionRequest? request = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unauthorized bot session create attempt. Invalid user id claim: {UserIdClaim}. Name: {Name}, Email: {Email}", userIdClaim, userNameClaim, emailClaim);
            return Unauthorized(new { message = "Invalid user token" });
        }

        _logger.LogInformation("User {UserId} ({Name}) requests bot session. InitialMessage present: {HasInitial}", userId, userNameClaim, !string.IsNullOrEmpty(request?.InitialMessage));

        try
        {
            var session = await _chatService.CreateBotSessionAsync(userId, request ?? new CreateBotSessionRequest());
            if (session == null)
            {
                _logger.LogWarning("Failed to create bot session for user id {UserId} - user not found or cannot create session.", userId);
                return NotFound(new { message = "Người dùng không tồn tại hoặc không thể tạo phiên chat với bot" });
            }

            return Ok(new
            {
                maPhienChat = session.MaPhienChat,
                coBot = session.CoBot,
                message = "Phiên chat với bot đã được tạo thành công. Sử dụng endpoint /api/Chat/bot/message/send để gửi tin nhắn."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bot session for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiên chat với bot" });
        }
    }

    [Authorize]
    [HttpPost("message/send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var msg = await _chatService.SendMessageAsync(userId, request);
        if (msg == null) return BadRequest(new { message = "Phiên chat không tồn tại" });

        return Ok(new MessageDto
        {
            MaTinNhan = msg.MaTinNhan,
            MaPhienChat = msg.MaPhienChat,
            MaNguoiGui = msg.MaNguoiGui,
            NoiDung = msg.NoiDung,
            ThoiGianGui = msg.ThoiGianGui,
            LaBot = msg.LaBot
        });
    }

    [Authorize]
    [HttpPost("bot/message/send")]
    public async Task<IActionResult> SendMessageToBot([FromBody] SendMessageRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var response = await _chatService.SendMessageToBotAsync(userId, request);
            if (response == null)
                return BadRequest(new
                {
                    message = "Phiên chat bot không tồn tại hoặc không hợp lệ. Đảm bảo bạn đã tạo phiên chat bot bằng /api/Chat/bot-session/create"
                });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to bot for user {UserId}, session {SessionId}", userId, request.MaPhienChat);
            return StatusCode(500, new { message = "Lỗi hệ thống khi gửi tin nhắn đến bot" });
        }
    }

    [Authorize]
    [HttpPost("bot/test")]
    public async Task<IActionResult> TestBotConnection()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            _logger.LogInformation("Testing bot connection for user {UserId}", userId);

            var testMessage = "Xin chào! Đây là tin nhắn test.";
            var botResponse = await _chatService.TestBotDirectly(testMessage, userId.ToString());

            return Ok(new
            {
                success = true,
                testMessage = testMessage,
                botResponse = botResponse,
                message = "Bot connection test completed. Check logs for details."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing bot connection for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi khi test kết nối bot", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("staff/request")]
    public async Task<IActionResult> RequestStaff([FromBody] RequestStaffRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var success = await _chatService.RequestStaffAsync(userId, request);
        if (!success)
            return BadRequest(new { message = "Không thể yêu cầu hỗ trợ từ nhân viên" });

        return Ok(new
        {
            message = "Đã yêu cầu hỗ trợ từ nhân viên thành công. Vui lòng chờ nhân viên tham gia.",
            success = true
        });
    }

    [Authorize]
    [HttpGet("messages/{maPhienChat}")]
    public async Task<IActionResult> GetMessages(int maPhienChat)
    {
        var messages = await _chatService.GetMessagesAsync(maPhienChat);
        return Ok(messages);
    }

    [HttpGet("test-bot")]
    public async Task<IActionResult> TestBot()
    {
        var botResponse = await _chatService.TestBotDirectly("Xin chào, bạn là ai?", "user123");
        return Ok(botResponse);
    }

    /// <summary>
    /// Webhook endpoint để nhận tin nhắn từ Botpress
    /// URL này sẽ được cấu hình trong Botpress webhook settings
    /// </summary>
    [HttpPost("webhook/botpress")]
    public async Task<IActionResult> BotpressWebhook([FromBody] JsonElement webhookData)
    {
        try
        {
            _logger.LogInformation("Received Botpress webhook: {WebhookData}", webhookData.GetRawText());

            // Gọi service để xử lý webhook
            await _botpressService.HandleWebhookAsync(webhookData);

            return Ok(new { status = "success", message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Botpress webhook");
            return StatusCode(500, new { status = "error", message = "Failed to process webhook" });
        }
    }

    /// <summary>
    /// Test endpoint để kiểm tra webhook (có thể xóa sau khi test xong)
    /// </summary>
    [HttpPost("webhook/test")]
    public async Task<IActionResult> TestWebhook([FromBody] JsonElement testData)
    {
        try
        {
            _logger.LogInformation("Test webhook received: {TestData}", testData.GetRawText());
            
            // Test với dữ liệu mẫu
            var sampleWebhookData = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "user": {
                        "id": "123"
                    },
                    "payload": {
                        "text": "Đây là tin nhắn test từ webhook"
                    }
                }
                """);

            await _botpressService.HandleWebhookAsync(sampleWebhookData);

            return Ok(new { 
                status = "success", 
                message = "Test webhook processed successfully",
                receivedData = testData.GetRawText()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test webhook");
            return StatusCode(500, new { status = "error", message = "Failed to process test webhook" });
        }
    }

    /// <summary>
    /// Debug endpoint để kiểm tra bot response có được lưu vào database không
    /// </summary>
    [HttpPost("debug/bot-message")]
    public async Task<IActionResult> DebugBotMessage([FromBody] DebugBotMessageRequest request)
    {
        try
        {
            _logger.LogInformation("Debug: Testing bot message save for user {UserId}", request.UserId);
            
            // Test direct bot communication
            var botResponse = await _botpressService.SendMessageToBotAsync(request.Message, request.UserId);
            
            // Check if message was saved
            var savedMessages = await _chatService.GetMessagesAsync(request.SessionId ?? 0);
            var botMessages = savedMessages.Where(m => m.LaBot == true).OrderByDescending(m => m.ThoiGianGui).Take(3);
            
            return Ok(new { 
                success = true,
                botResponse = botResponse,
                recentBotMessages = botMessages,
                message = "Bot message test completed. Check recentBotMessages to see if bot response was saved."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debug bot message test");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class DebugBotMessageRequest
{
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";
    public int? SessionId { get; set; }
}
