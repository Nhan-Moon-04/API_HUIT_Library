using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HUIT_Library.Services
{
    public class ChatService : IChatService
    {
        private readonly HuitThuVienContext _context;
        private readonly IBotpressService _botpressService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(HuitThuVienContext context, IBotpressService botpressService, ILogger<ChatService> logger)
        {
            _context = context;
            _botpressService = botpressService;
            _logger = logger;
        }

        // Regular chat session creation (without bot)
        public async Task<PhienChat?> CreateSessionAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Attempting to create regular session for user ID: {UserId}", userId);

                var user = await _context.NguoiDungs.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("User found: {UserName} ({UserId})", user.HoTen, userId);

                var session = new PhienChat
                {
                    MaNguoiDung = userId,
                    MaNhanVien = null,
                    CoBot = false, // Regular chat without bot
                    ThoiGianBatDau = DateTime.UtcNow
                };

                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created regular session {SessionId} for user {UserId}", session.MaPhienChat, userId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating regular session for user {UserId}", userId);
                return null;
            }
        }

        // Create bot session - this automatically enables the user to chat with bot
        public async Task<PhienChat?> CreateBotSessionAsync(int userId, CreateBotSessionRequest request)
        {
            try
            {
                _logger.LogInformation("Creating bot session for user {UserId} with initial message: {HasInitial}", 
                    userId, !string.IsNullOrEmpty(request?.InitialMessage));

                var user = await _context.NguoiDungs.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot create bot session: user with ID {UserId} not found.", userId);
                    return null;
                }

                // Create bot session in database
                var session = new PhienChat
                {
                    MaNguoiDung = userId,
                    MaNhanVien = null,
                    CoBot = true, // This session includes bot
                    ThoiGianBatDau = DateTime.UtcNow
                };

                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created bot session {SessionId} for user {UserId}", session.MaPhienChat, userId);

                // Send initial message or welcome message
                string initialMessage = request?.InitialMessage ?? "Xin chào! Tôi có thể giúp gì cho bạn?";
                
                if (!string.IsNullOrEmpty(request?.InitialMessage))
                {
                    // User provided initial message - send it to bot and get response
                    _logger.LogInformation("Sending user's initial message to bot: {Message}", request.InitialMessage);
                    
                    // Save user's initial message
                    var userMessage = new TinNhan
                    {
                        MaPhienChat = session.MaPhienChat,
                        MaNguoiGui = userId,
                        NoiDung = request.InitialMessage,
                        ThoiGianGui = DateTime.UtcNow,
                        LaBot = false
                    };
                    _context.TinNhans.Add(userMessage);
                    await _context.SaveChangesAsync();

                    // Get bot response and save it
                    var botResponse = await _botpressService.SendMessageToBotAsync(request.InitialMessage, userId.ToString());
                    await _botpressService.ProcessBotResponseAsync(botResponse, session.MaPhienChat);
                }
                else
                {
                    // Send default welcome message from bot
                    _logger.LogInformation("Sending default welcome message from bot");
                    await _botpressService.ProcessBotResponseAsync(initialMessage, session.MaPhienChat);
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bot session for user {UserId}", userId);
                return null;
            }
        }

        // Send message to bot and get response
        public async Task<BotResponseDto?> SendMessageToBotAsync(int userId, SendMessageRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} sending message to bot in session {SessionId}: {Message}", 
                    userId, request.MaPhienChat, request.NoiDung);

                var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
                if (session == null || session.MaNguoiDung != userId || session.CoBot != true)
                {
                    _logger.LogWarning("Invalid bot session {SessionId} for user {UserId}", request.MaPhienChat, userId);
                    return null;
                }

                // Verify user exists
                var sender = await _context.NguoiDungs.FindAsync(userId);
                if (sender == null) 
                {
                    _logger.LogWarning("User {UserId} not found when sending message to bot", userId);
                    return null;
                }

                // Save user message to database
                var userMessage = new TinNhan
                {
                    MaPhienChat = request.MaPhienChat,
                    MaNguoiGui = userId,
                    NoiDung = request.NoiDung,
                    ThoiGianGui = DateTime.UtcNow,
                    LaBot = false
                };

                _context.TinNhans.Add(userMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved user message {MessageId} to database", userMessage.MaTinNhan);

                // Send to bot and get response
                var botResponseText = await _botpressService.SendMessageToBotAsync(request.NoiDung, userId.ToString());
                var botMessage = await _botpressService.ProcessBotResponseAsync(botResponseText, request.MaPhienChat);

                // Check if user is asking for staff assistance
                var requiresStaff = CheckIfRequiresStaff(request.NoiDung) || CheckIfRequiresStaff(botResponseText);

                _logger.LogInformation("Bot conversation completed. RequiresStaff: {RequiresStaff}", requiresStaff);

                return new BotResponseDto
                {
                    UserMessage = new MessageDto
                    {
                        MaTinNhan = userMessage.MaTinNhan,
                        MaPhienChat = userMessage.MaPhienChat,
                        MaNguoiGui = userMessage.MaNguoiGui,
                        NoiDung = userMessage.NoiDung,
                        ThoiGianGui = userMessage.ThoiGianGui,
                        LaBot = userMessage.LaBot
                    },
                    BotMessage = botMessage,
                    RequiresStaff = requiresStaff,
                    StaffRequestReason = requiresStaff ? "Bot đề xuất chuyển đến nhân viên" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bot conversation for user {UserId}, session {SessionId}", userId, request.MaPhienChat);
                return null;
            }
        }

        // Request staff assistance
        public async Task<bool> RequestStaffAsync(int userId, RequestStaffRequest request)
        {
            try
            {
                var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
                if (session == null || session.MaNguoiDung != userId)
                {
                    _logger.LogWarning("Cannot request staff: invalid session {SessionId} for user {UserId}", request.MaPhienChat, userId);
                    return false;
                }

                // Update session to disable bot and prepare for staff
                session.CoBot = false;
                // MaNhanVien will be assigned when a staff member joins

                // Add system message about staff request
                var systemMessage = new TinNhan
                {
                    MaPhienChat = request.MaPhienChat,
                    MaNguoiGui = 0, // System message
                    NoiDung = $"Người dùng đã yêu cầu hỗ trợ từ nhân viên. Lý do: {request.LyDo ?? "Không có lý do cụ thể"}",
                    ThoiGianGui = DateTime.UtcNow,
                    LaBot = false
                };

                _context.TinNhans.Add(systemMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} requested staff assistance for session {SessionId}", userId, request.MaPhienChat);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting staff for user {UserId}, session {SessionId}", userId, request.MaPhienChat);
                return false;
            }
        }

        // Send message in regular chat (non-bot)
        public async Task<TinNhan?> SendMessageAsync(int userId, SendMessageRequest request)
        {
            try
            {
                var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
                if (session == null) 
                {
                    _logger.LogWarning("Session {SessionId} not found for user {UserId}", request.MaPhienChat, userId);
                    return null;
                }

                // Verify sender exists
                var sender = await _context.NguoiDungs.FindAsync(userId);
                if (sender == null) 
                {
                    _logger.LogWarning("User {UserId} not found when sending message", userId);
                    return null;
                }

                var message = new TinNhan
                {
                    MaPhienChat = request.MaPhienChat,
                    MaNguoiGui = userId,
                    NoiDung = request.NoiDung,
                    ThoiGianGui = DateTime.UtcNow,
                    LaBot = false
                };

                _context.TinNhans.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved regular message {MessageId} from user {UserId}", message.MaTinNhan, userId);

                // If this session has a bot enabled, forward the message to the bot
                if (session.CoBot == true)
                {
                    try
                    {
                        var botResponseText = await _botpressService.SendMessageToBotAsync(request.NoiDung, userId.ToString());
                        await _botpressService.ProcessBotResponseAsync(botResponseText, request.MaPhienChat);
                        _logger.LogInformation("Forwarded message to bot for session {SessionId}", request.MaPhienChat);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error forwarding message to bot for session {SessionId}", request.MaPhienChat);
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending regular message for user {UserId}, session {SessionId}", userId, request.MaPhienChat);
                return null;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int maPhienChat)
        {
            try
            {
                return await _context.TinNhans
                    .Where(t => t.MaPhienChat == maPhienChat)
                    .OrderBy(t => t.ThoiGianGui)
                    .Select(t => new MessageDto
                    {
                        MaTinNhan = t.MaTinNhan,
                        MaPhienChat = t.MaPhienChat,
                        MaNguoiGui = t.MaNguoiGui,
                        NoiDung = t.NoiDung,
                        ThoiGianGui = t.ThoiGianGui,
                        LaBot = t.LaBot
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for session {SessionId}", maPhienChat);
                return new List<MessageDto>();
            }
        }

        public async Task<object?> GetSessionInfoAsync(int maPhienChat, int userId)
        {
            try
            {
                var session = await _context.PhienChats.FindAsync(maPhienChat);
                if (session == null || session.MaNguoiDung != userId)
                {
                    _logger.LogWarning("Session {SessionId} not found or access denied for user {UserId}", maPhienChat, userId);
                    return null;
                }

                var messageCount = await _context.TinNhans.CountAsync(t => t.MaPhienChat == maPhienChat);

                return new
                {
                    maPhienChat = session.MaPhienChat,
                    maNguoiDung = session.MaNguoiDung,
                    maNhanVien = session.MaNhanVien,
                    coBot = session.CoBot,
                    thoiGianBatDau = session.ThoiGianBatDau,
                    thoiGianKetThuc = session.ThoiGianKetThuc,
                    soLuongTinNhan = messageCount,
                    loaiPhien = session.CoBot == true ? "Bot Chat" : "Regular Chat",
                    endpointGoiY = session.CoBot == true ? "/api/Chat/bot/message/send" : "/api/Chat/message/send"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session info for session {SessionId}", maPhienChat);
                return null;
            }
        }

        private bool CheckIfRequiresStaff(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            var staffKeywords = new[] {
                "nhân viên", "nhan vien", "staff", "hỗ trợ", "ho tro",
                "giúp đỡ", "giup do", "không hiểu", "khong hieu",
                "phức tạp", "phuc tap", "gặp trực tiếp", "gap truc tiep"
            };

            return staffKeywords.Any(keyword => message.ToLower().Contains(keyword.ToLower()));
        }

        // Test method for diagnosing bot issues
        public async Task<string> TestBotDirectly(string message, string userId)
        {
            _logger.LogInformation("=== DIRECT BOT TEST ===");
            _logger.LogInformation("Test message: {Message}", message);
            _logger.LogInformation("User ID: {UserId}", userId);

            try
            {
                var response = await _botpressService.SendMessageToBotAsync(message, userId);
                _logger.LogInformation("Bot response received: {Response}", response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct bot test");
                return $"Lỗi trong test: {ex.Message}";
            }
        }
    }
}
