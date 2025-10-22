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
                return BadRequest(new { message = "Phiên chat không tồn tại hoặc bạn không có quyền truy cập" });

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
                return BadRequest(new { message = "Không thể tạo phiên chat" });
            }

            _logger.LogInformation("Successfully created chat session {SessionId} for user {UserId}", session.MaPhienChat, userId);
            return Ok(new
            {
                maPhienChat = session.MaPhienChat,
                coBot = session.CoBot,
                message = "Phiên chat thường đã được tạo thành công"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiên chat" });
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
    /// Get latest chat session with complete message history for current user
    /// Tự động lấy phiên chat mới nhất của user và hiển thị toàn bộ lịch sử chat
    /// Nếu chưa có phiên chat nào thì tự động tạo phiên chat bot mới
    /// </summary>
    [Authorize]
    [HttpGet("user/latest-with-messages")]
    public async Task<IActionResult> GetLatestChatSessionWithMessages()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim when getting latest chat session: {UserIdClaim}", userIdClaim);
            return Unauthorized(new { message = "Token không hợp lệ" });
        }

        try
        {
            _logger.LogInformation("Getting latest chat session with messages for user {UserId} ({UserName})", userId, userNameClaim);

            var latestSessionWithMessages = await _chatService.GetLatestChatSessionWithMessagesAsync(userId);

            if (latestSessionWithMessages is null)
            {
                return StatusCode(500, new
                {
                    success = false,
                    data = (object?)null,
                    message = "Không thể lấy hoặc tạo phiên chat cho người dùng"
                });
            }

            // Check if this is a newly created session (has only bot welcome message)
            bool isNewSession = latestSessionWithMessages.TotalMessages == 1 && 
                               latestSessionWithMessages.Messages.Any(m => m.LaBot == true);

            var message = isNewSession 
                ? $"Đã tự động tạo phiên chat bot mới (ID: {latestSessionWithMessages.SessionInfo.MaPhienChat}) với tin nhắn chào mừng"
                : $"Đã tải thành công phiên chat mới nhất (ID: {latestSessionWithMessages.SessionInfo.MaPhienChat}) với {latestSessionWithMessages.TotalMessages} tin nhắn";

            return Ok(new
            {
                success = true,
                data = latestSessionWithMessages,
                isNewSession = isNewSession,
                message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest chat session with messages for user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi hệ thống khi tải phiên chat mới nhất",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Load all chat sessions for current user (for when user logs in)
    /// </summary>
    [Authorize]
    [HttpGet("user/sessions")]
    public async Task<IActionResult> GetUserChatSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var sessions = await _chatService.GetUserChatSessionsAsync(userId);
            
            return Ok(new {
                success = true,
                userId = userId,
                sessions = sessions,
                totalSessions = sessions.Count(),
                message = "Chat sessions loaded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tải phiên chat" });
        }
    }

    /// <summary>
    /// Get or create bot session for current user
    /// </summary>
    [Authorize]
    [HttpGet("user/bot-session")]
    public async Task<IActionResult> GetOrCreateBotSession()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var botSession = await _chatService.GetOrCreateBotSessionAsync(userId);
            
            if (botSession == null)
            {
                return BadRequest(new { message = "Không thể tạo hoặc lấy phiên chat bot" });
            }

            return Ok(new {
                success = true,
                botSession = botSession,
                message = "Bot session ready"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting/creating bot session for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiên chat bot" });
        }
    }

    /// <summary>
    /// Get recent messages for a session with pagination
    /// </summary>
    [Authorize]
    [HttpGet("session/{maPhienChat}/messages")]
    public async Task<IActionResult> GetRecentMessages(int maPhienChat, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var messagesPage = await _chatService.GetRecentMessagesAsync(maPhienChat, userId, page, pageSize);
            
            return Ok(new {
                success = true,
                data = messagesPage,
                message = $"Loaded {messagesPage.Messages.Count()} messages for session {maPhienChat}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading messages for session {SessionId}, user {UserId}", maPhienChat, userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tải tin nhắn" });
        }
    }

    /// <summary>
    /// Dashboard endpoint: Load user data when they first login
    /// </summary>
    [Authorize]
    [HttpGet("user/dashboard")]
    public async Task<IActionResult> GetUserDashboard()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            _logger.LogInformation("Loading dashboard data for user {UserId} ({UserName})", userId, userNameClaim);

            // Load all user's chat sessions
            var allSessions = await _chatService.GetUserChatSessionsAsync(userId);
            
            // Get active bot session (if any)
            var activeBotSession = await _chatService.GetActiveBotSessionAsync(userId);
            
            // Split sessions by type
            var botSessions = allSessions.Where(s => s.CoBot == true).ToList();
            var regularSessions = allSessions.Where(s => s.CoBot != true).ToList();

            return Ok(new {
                success = true,
                user = new {
                    userId = userId,
                    userName = userNameClaim
                },
                dashboard = new {
                    totalSessions = allSessions.Count(),
                    botSessions = new {
                        total = botSessions.Count,
                        active = activeBotSession,
                        recent = botSessions.Take(5)
                    },
                    regularSessions = new {
                        total = regularSessions.Count,
                        recent = regularSessions.Take(5)
                    },
                    allSessions = allSessions.Take(10) // Recent 10 sessions
                },
                message = "Dashboard data loaded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {UserId}", userId);
            return StatusCode(500, new { message = "Lỗi hệ thống khi tải dashboard" });
        }
    }

    /// <summary>
    /// Debug endpoint to test Botpress connection
    /// </summary>
    [Authorize]
    [HttpPost("debug/test-bot")]
    public async Task<IActionResult> TestBotConnection([FromBody] TestBotRequest? request = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var testMessage = request?.Message ?? "test";
            _logger.LogInformation("Testing bot connection for user {UserId} with message: {Message}", userId, testMessage);

            var botResponse = await _botpressService.SendMessageToBotAsync(testMessage, userId.ToString());
            
            return Ok(new {
                success = true,
                userId = userId,
                testMessage = testMessage,
                botResponse = botResponse,
                message = "Bot connection test completed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing bot connection for user {UserId}", userId);
            return StatusCode(500, new { 
                success = false,
                message = "Bot connection test failed", 
                error = ex.Message 
            });
        }
    }
}
