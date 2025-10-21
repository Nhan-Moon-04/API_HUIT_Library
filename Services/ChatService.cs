using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace HUIT_Library.Services
{
    public class ChatService : IChatService
    {
        private readonly HuitThuVienContext _context;

        public ChatService(HuitThuVienContext context)
        {
            _context = context;
        }

        // Người dùng (user) là người tạo chat trước
        public async Task<PhienChat?> CreateSessionAsync(int userId)
        {
            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null)
                return null;

            // Không gán MaNhanVien, vì đây là phòng chat chung
            var session = new PhienChat
            {
                MaNguoiDung = userId,
                MaNhanVien = null,  // ❌ chưa có nhân viên nào
                CoBot = false,
                ThoiGianBatDau = DateTime.UtcNow
            };

            _context.PhienChats.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        // Gửi tin nhắn trong chat
        public async Task<TinNhan?> SendMessageAsync(int userId, SendMessageRequest request)
        {
            var session = await _context.PhienChats.FindAsync(request.MaPhienChat);
            if (session == null) return null;

            // Xác minh người gửi tồn tại
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
    }
}
