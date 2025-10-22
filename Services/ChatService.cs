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

        // Helper method to get Vietnam timezone
        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
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
                    ThoiGianBatDau = GetVietnamTime()
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
                    ThoiGianBatDau = GetVietnamTime()
                };

                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created bot session {SessionId} for user {UserId}", session.MaPhienChat, userId);

                // Initialize Botpress conversation
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
                        ThoiGianGui = GetVietnamTime(),
                        LaBot = false
                    };
                    _context.TinNhans.Add(userMessage);
                    await _context.SaveChangesAsync();

                    // Get bot response and save it (this will initialize the conversation)
                    var botResponse = await _botpressService.SendMessageToBotAsync(request.InitialMessage, userId.ToString());
                    await _botpressService.ProcessBotResponseAsync(botResponse, session.MaPhienChat);
                }
                else
                {
                    // Initialize conversation with Botpress by sending a simple greeting
                    _logger.LogInformation("Initializing Botpress conversation for user {UserId}", userId);
                    
                    try
                    {
                        // Try to initialize with a simple greeting - this should create the conversation
                        var botResponse = await _botpressService.SendMessageToBotAsync("Hi", userId.ToString());
                        
                        // If we get a valid response, save it. Otherwise, use a fallback
                        if (!string.IsNullOrEmpty(botResponse) && !botResponse.Contains("bot đang bận"))
                        {
                            await _botpressService.ProcessBotResponseAsync(botResponse, session.MaPhienChat);
                            _logger.LogInformation("Botpress conversation initialized successfully for user {UserId} in session {SessionId}", userId, session.MaPhienChat);
                        }
                        else
                        {
                            // Fallback: save a welcome message manually
                            _logger.LogWarning("Botpress initialization failed for user {UserId}, using fallback welcome message", userId);
                            await _botpressService.ProcessBotResponseAsync("Xin chào! Tôi có thể giúp gì cho bạn?", session.MaPhienChat);
                            
                            // Try to initialize conversation in background - don't wait for it
                            _ = Task.Run(async () => 
                            {
                                try
                                {
                                    await Task.Delay(2000); // Wait 2 seconds
                                    await _botpressService.SendMessageToBotAsync("Xin chào", userId.ToString());
                                    _logger.LogInformation("Background Botpress initialization completed for user {UserId}", userId);
                                }
                                catch (Exception bgEx)
                                {
                                    _logger.LogError(bgEx, "Background Botpress initialization failed for user {UserId}", userId);
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error initializing Botpress conversation for user {UserId}, using fallback", userId);
                        // Fallback: save a welcome message manually
                        await _botpressService.ProcessBotResponseAsync("Xin chào! Tôi có thể giúp gì cho bạn?", session.MaPhienChat);
                    }
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
                    ThoiGianGui = GetVietnamTime(),
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
                    ThoiGianGui = GetVietnamTime(),
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
                    ThoiGianGui = GetVietnamTime(),
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

        // Load user's chat sessions when user logs in
        public async Task<IEnumerable<ChatSessionDto>> GetUserChatSessionsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Loading chat sessions for user {UserId}", userId);

                var sessions = await _context.PhienChats
                    .Where(p => p.MaNguoiDung == userId)
                    .OrderByDescending(p => p.ThoiGianBatDau)
                    .Select(p => new ChatSessionDto
                    {
                        MaPhienChat = p.MaPhienChat,
                        MaNguoiDung = p.MaNguoiDung,
                        MaNhanVien = p.MaNhanVien,
                        CoBot = p.CoBot,
                        ThoiGianBatDau = p.ThoiGianBatDau ?? GetVietnamTime(),
                        ThoiGianKetThuc = p.ThoiGianKetThuc,
                        SoLuongTinNhan = _context.TinNhans.Count(t => t.MaPhienChat == p.MaPhienChat),
                        TinNhanCuoi = _context.TinNhans
                            .Where(t => t.MaPhienChat == p.MaPhienChat)
                            .OrderByDescending(t => t.ThoiGianGui)
                            .Select(t => t.NoiDung)
                            .FirstOrDefault(),
                        ThoiGianTinNhanCuoi = _context.TinNhans
                            .Where(t => t.MaPhienChat == p.MaPhienChat)
                            .OrderByDescending(t => t.ThoiGianGui)
                            .Select(t => t.ThoiGianGui)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} chat sessions for user {UserId}", sessions.Count, userId);
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat sessions for user {UserId}", userId);
                return new List<ChatSessionDto>();
            }
        }

        // Get active bot session for user (if exists)
        public async Task<ChatSessionDto?> GetActiveBotSessionAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Looking for active bot session for user {UserId}", userId);

                var session = await _context.PhienChats
                    .Where(p => p.MaNguoiDung == userId && p.CoBot == true && p.ThoiGianKetThuc == null)
                    .OrderByDescending(p => p.ThoiGianBatDau)
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    _logger.LogInformation("No active bot session found for user {UserId}", userId);
                    return null;
                }

                var sessionDto = new ChatSessionDto
                {
                    MaPhienChat = session.MaPhienChat,
                    MaNguoiDung = session.MaNguoiDung,
                    MaNhanVien = session.MaNhanVien,
                    CoBot = session.CoBot,
                    ThoiGianBatDau = session.ThoiGianBatDau ?? GetVietnamTime(),
                    ThoiGianKetThuc = session.ThoiGianKetThuc,
                    SoLuongTinNhan = await _context.TinNhans.CountAsync(t => t.MaPhienChat == session.MaPhienChat),
                    TinNhanCuoi = await _context.TinNhans
                        .Where(t => t.MaPhienChat == session.MaPhienChat)
                        .OrderByDescending(t => t.ThoiGianGui)
                        .Select(t => t.NoiDung)
                        .FirstOrDefaultAsync(),
                    ThoiGianTinNhanCuoi = await _context.TinNhans
                        .Where(t => t.MaPhienChat == session.MaPhienChat)
                        .OrderByDescending(t => t.ThoiGianGui)
                        .Select(t => t.ThoiGianGui)
                        .FirstOrDefaultAsync()
                };

                _logger.LogInformation("Found active bot session {SessionId} for user {UserId}", session.MaPhienChat, userId);
                return sessionDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active bot session for user {UserId}", userId);
                return null;
            }
        }

        // Get or create bot session for user
        public async Task<ChatSessionDto?> GetOrCreateBotSessionAsync(int userId)
        {
            try
            {
                // First check if user has an active bot session
                var activeSession = await GetActiveBotSessionAsync(userId);
                if (activeSession != null)
                {
                    _logger.LogInformation("Returning existing bot session {SessionId} for user {UserId}", activeSession.MaPhienChat, userId);
                    return activeSession;
                }

                // Create new bot session if none exists
                _logger.LogInformation("Creating new bot session for user {UserId}", userId);
                var newSession = await CreateBotSessionAsync(userId, new CreateBotSessionRequest());
                
                if (newSession == null)
                {
                    _logger.LogWarning("Failed to create bot session for user {UserId}", userId);
                    return null;
                }

                // Return the new session as DTO
                return new ChatSessionDto
                {
                    MaPhienChat = newSession.MaPhienChat,
                    MaNguoiDung = newSession.MaNguoiDung,
                    MaNhanVien = newSession.MaNhanVien,
                    CoBot = newSession.CoBot,
                    ThoiGianBatDau = newSession.ThoiGianBatDau ?? GetVietnamTime(),
                    ThoiGianKetThuc = newSession.ThoiGianKetThuc,
                    SoLuongTinNhan = 1, // Welcome message
                    TinNhanCuoi = "Xin chào! Tôi có thể giúp gì cho bạn?",
                    ThoiGianTinNhanCuoi = newSession.ThoiGianBatDau
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating bot session for user {UserId}", userId);
                return null;
            }
        }

        // Get recent messages for a session with pagination
        public async Task<ChatMessagesPageDto> GetRecentMessagesAsync(int maPhienChat, int userId, int page = 1, int pageSize = 50)
        {
            try
            {
                // Verify user has access to this session
                var session = await _context.PhienChats.FindAsync(maPhienChat);
                if (session == null || session.MaNguoiDung != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access unauthorized session {SessionId}", userId, maPhienChat);
                    return new ChatMessagesPageDto { Messages = new List<MessageDto>(), TotalCount = 0 };
                }

                var skip = (page - 1) * pageSize;
                var totalCount = await _context.TinNhans.CountAsync(t => t.MaPhienChat == maPhienChat);

                var messages = await _context.TinNhans
                    .Where(t => t.MaPhienChat == maPhienChat)
                    .OrderByDescending(t => t.ThoiGianGui) // Get newest first
                    .Skip(skip)
                    .Take(pageSize)
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

                // Reverse to show chronological order (oldest to newest)
                messages.Reverse();

                _logger.LogInformation("Loaded {Count} messages for session {SessionId}, page {Page}", messages.Count, maPhienChat, page);

                return new ChatMessagesPageDto
                {
                    Messages = messages,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = skip + pageSize < totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent messages for session {SessionId}, user {UserId}", maPhienChat, userId);
                return new ChatMessagesPageDto { Messages = new List<MessageDto>(), TotalCount = 0 };
            }
        }

        public async Task<int> GetLastest_PhienChatUser(int userId)
        {
            try
            {
                var session = await _context.PhienChats
                    .Where(p => p.MaNguoiDung == userId)
                    .OrderByDescending(p => p.ThoiGianBatDau)
                    .FirstOrDefaultAsync();
                if (session == null)
                {
                    _logger.LogInformation("No chat sessions found for user {UserId}", userId);
                    return 0; // No sessions found
                }
                _logger.LogInformation("Latest chat session for user {UserId} is {SessionId}", userId, session.MaPhienChat);
                return session.MaPhienChat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest chat session for user {UserId}", userId);
                return 0;
            }
        }

        // Get latest chat session with full message history
        // Auto-create bot session if user has no sessions
        public async Task<ChatSessionWithMessagesDto?> GetLatestChatSessionWithMessagesAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting latest chat session with messages for user {UserId}", userId);

                // Get latest session
                var latestSession = await _context.PhienChats
                    .Where(p => p.MaNguoiDung == userId)
                    .OrderByDescending(p => p.ThoiGianBatDau)
                    .FirstOrDefaultAsync();

                // If user has no sessions, auto-create a bot session
                if (latestSession == null)
                {
                    _logger.LogInformation("User {UserId} has no chat sessions, auto-creating bot session", userId);
                    
                    // Check if user exists
                    var user = await _context.NguoiDungs.FindAsync(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("Cannot create auto bot session: user with ID {UserId} not found", userId);
                        return null;
                    }

                    // Create bot session with welcome message
                    var newBotSession = await CreateBotSessionAsync(userId, new CreateBotSessionRequest
                    {
                        InitialMessage = null // This will trigger the default welcome message
                    });

                    if (newBotSession == null)
                    {
                        _logger.LogWarning("Failed to auto-create bot session for user {UserId}", userId);
                        return null;
                    }

                    latestSession = newBotSession;
                    _logger.LogInformation("Auto-created bot session {SessionId} for user {UserId}", latestSession.MaPhienChat, userId);
                }

                // Get all messages for this session
                var messages = await _context.TinNhans
                    .Where(t => t.MaPhienChat == latestSession.MaPhienChat)
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

                // Create session DTO with messages
                var result = new ChatSessionWithMessagesDto
                {
                    SessionInfo = new ChatSessionDto
                    {
                        MaPhienChat = latestSession.MaPhienChat,
                        MaNguoiDung = latestSession.MaNguoiDung,
                        MaNhanVien = latestSession.MaNhanVien,
                        CoBot = latestSession.CoBot,
                        ThoiGianBatDau = latestSession.ThoiGianBatDau ?? GetVietnamTime(),
                        ThoiGianKetThuc = latestSession.ThoiGianKetThuc,
                        SoLuongTinNhan = messages.Count,
                        TinNhanCuoi = messages.LastOrDefault()?.NoiDung,
                        ThoiGianTinNhanCuoi = messages.LastOrDefault()?.ThoiGianGui
                    },
                    Messages = messages,
                    TotalMessages = messages.Count
                };

                _logger.LogInformation("Found/created session {SessionId} with {MessageCount} messages for user {UserId}", 
                    latestSession.MaPhienChat, messages.Count, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating latest chat session with messages for user {UserId}", userId);
                return null;
            }
        }
    }
}
