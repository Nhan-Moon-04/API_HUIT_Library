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

        // Ngu?i dùng (user) là ngu?i t?o chat tru?c - regular chat without bot
        public async Task<PhienChat?> CreateSessionAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Attempting to create session for user ID: {UserId}", userId);

                var user = await _context.NguoiDungs.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("User found: {UserName} ({UserId})", user.HoTen, userId);

                // Không gán MaNhanVien, vì dây là phòng chat chung
                var session = new PhienChat
                {
                    MaNguoiDung = userId,
                    MaNhanVien = null,  // ? chua có nhân viên nào
                    CoBot = false,
                    ThoiGianBatDau = DateTime.UtcNow
                };

                _context.PhienChats.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created session {SessionId} for user {UserId}", session.MaPhienChat, userId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for user {UserId}", userId);
                return null;
            }
        }

        // Create bot session when user wants to chat with bot
        public async Task<PhienChat?> CreateBotSessionAsync(int userId, CreateBotSessionRequest request)
        {
            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null)
                return null;

            var session = new PhienChat
            {
                MaNguoiDung = userId,
                MaNhanVien = null,
                CoBot = true, // This session includes bot
                ThoiGianBatDau = DateTime.UtcNow
            };

            _context.PhienChats.Add(session);
            await _context.SaveChangesAsync();

            // Send initial message if provided
            if (!string.IsNullOrEmpty(request.InitialMessage))
            {
                await SendMessageToBotAsync(userId, new SendMessageRequest
                {
                    MaPhienChat = session.MaPhienChat,
                    NoiDung = request.InitialMessage
                });
            }
            else
            {
                // Send default welcome message from bot
                var botResponse = await _botpressService.SendMessageToBotAsync("Xin chào! Tôi có thể giúp gì cho bạn?", userId.ToString());
                await _botpressService.ProcessBotResponseAsync(botResponse, session.MaPhienChat);
            }

            return session;
        }

        // Send message to bot and get response
        public async Task<BotResponseDto?> SendMessageToBotAsync(int userId, SendMessageRequest request)
        {
            var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
            if (session == null || session.MaNguoiDung != userId || session.CoBot != true)
                return null;

            // Xác minh ngu?i g?i t?n t?i
            var sender = await _context.NguoiDungs.FindAsync(userId);
            if (sender == null) return null;

            // Save user message
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

            // Send to bot and get response
            var botResponseText = await _botpressService.SendMessageToBotAsync(request.NoiDung, userId.ToString());
            var botMessage = await _botpressService.ProcessBotResponseAsync(botResponseText, request.MaPhienChat);

            // Check if user is asking for staff assistance
            var requiresStaff = CheckIfRequiresStaff(request.NoiDung) || CheckIfRequiresStaff(botResponseText);

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

        // Request staff assistance
        public async Task<bool> RequestStaffAsync(int userId, RequestStaffRequest request)
        {
            var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
            if (session == null || session.MaNguoiDung != userId)
                return false;

            // Update session to disable bot and prepare for staff
            session.CoBot = false;
            // MaNhanVien will be assigned when a staff member joins

            // Add system message about staff request
            var systemMessage = new TinNhan
            {
                MaPhienChat = request.MaPhienChat,
                MaNguoiGui = 0, // System message
                NoiDung = $"Người dùng dã yêu cầu hỗ trợ tới nhân viên. Lý do: {request.LyDo ?? "Không có lý do cụ thể"}",
                ThoiGianGui = DateTime.UtcNow,
                LaBot = false
            };

            _context.TinNhans.Add(systemMessage);
            await _context.SaveChangesAsync();

            return true;
        }

        // G?i tin nh?n trong chat (regular chat)
        public async Task<TinNhan?> SendMessageAsync(int userId, SendMessageRequest request)
        {
            var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
            if (session == null) return null;

            // Xác minh ngu?i g?i t?n t?i
            var sender = await _context.NguoiDungs.FindAsync(userId);
            if (sender == null) return null;

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

            return message;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int maPhienChat)
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

        public async Task<object?> GetSessionInfoAsync(int maPhienChat, int userId)
        {
            try
            {
                var session = await _context.PhienChats.FindAsync(maPhienChat);
                if (session == null || session.MaNguoiDung != userId)
                    return null;

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
                "nhân viên", "nhan vien", "staff", "h? tr?", "ho tro",
                "giúp d?", "giup do", "không hi?u", "khong hieu",
                "ph?c t?p", "phuc tap", "g?p tr?c ti?p", "gap truc tiep"
            };

            return staffKeywords.Any(keyword => message.ToLower().Contains(keyword.ToLower()));
        }
    }
}
