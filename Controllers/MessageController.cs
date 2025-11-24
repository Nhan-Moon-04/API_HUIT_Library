using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HUIT_Library.Hubs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HUIT_Library.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly HuitThuVienContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessageController> _logger;

        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public MessageController(HuitThuVienContext context, IHubContext<ChatHub> hubContext, ILogger<MessageController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        private DateTime GetVietnamTime() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
     
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (!int.TryParse(userIdClaim, out int userId))
        throw new UnauthorizedAccessException("Không th? xác ??nh ng??i dùng.");
      return userId;
        }

     private string GetCurrentUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("NhanVien");

    [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendRealtimeMessageRequest request)
        {
   try
        {
  var senderId = GetCurrentUserId();
          var senderName = GetCurrentUserName();

          if (string.IsNullOrWhiteSpace(request.Message))
     return BadRequest(new { success = false, message = "Tin nh?n không ???c ?? tr?ng" });

                var chatSession = await FindOrCreateChatSessionAsync(senderId, request.RecipientId);
        
    var newMessage = new TinNhan
         {
           MaPhienChat = chatSession.MaPhienChat,
        MaNguoiGui = senderId,
         NoiDung = request.Message.Trim(),
        ThoiGianGui = GetVietnamTime(),
     LaBot = false
                };

       _context.TinNhans.Add(newMessage);
await _context.SaveChangesAsync();

     var messageData = new
                {
      Id = newMessage.MaTinNhan,
       ChatSessionId = chatSession.MaPhienChat,
              SenderId = senderId,
               SenderName = senderName,
            RecipientId = request.RecipientId,
          Message = newMessage.NoiDung,
 Timestamp = newMessage.ThoiGianGui
       };

                if (request.RecipientId == 0)
           await _hubContext.Clients.Group("AdminGroup").SendAsync("ReceiveMessage", messageData);
      else
        await _hubContext.Clients.Group($"User_{request.RecipientId}").SendAsync("ReceiveMessage", messageData);

           return Ok(new { success = true, message = "Tin nh?n ?ã ???c g?i thành công", data = messageData });
            }
            catch (Exception ex)
        {
                _logger.LogError(ex, "Error sending message from User {SenderId} to {RecipientId}", GetCurrentUserId(), request.RecipientId);
     return StatusCode(500, new { success = false, message = "Có l?i x?y ra khi g?i tin nh?n" });
     }
     }

        [HttpGet("history/{chatSessionId}")]
        public async Task<IActionResult> GetMessageHistory(int chatSessionId, int pageNumber = 1, int pageSize = 50)
        {
      try
            {
    var userId = GetCurrentUserId();
         
        if (!await CanAccessChatSessionAsync(userId, chatSessionId))
          return Forbid("B?n không có quy?n truy c?p phiên chat này");

      var messages = await (from tm in _context.TinNhans
          join nd in _context.NguoiDungs on tm.MaNguoiGui equals nd.MaNguoiDung
   where tm.MaPhienChat == chatSessionId
        orderby tm.ThoiGianGui
 select new
        {
Id = tm.MaTinNhan,
       SenderId = tm.MaNguoiGui,
           SenderName = nd.HoTen,
   Content = tm.NoiDung,
    Timestamp = tm.ThoiGianGui
          })
                .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
        .ToListAsync();

     return Ok(new { success = true, data = messages });
   }
        catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting message history for chat session {ChatSessionId}", chatSessionId);
     return StatusCode(500, new { success = false, message = "Có l?i x?y ra" });
          }
        }

        [HttpGet("chat-sessions")]
      public async Task<IActionResult> GetChatSessions()
        {
     try
    {
       var userId = GetCurrentUserId();
          var isAdmin = IsAdmin();

     var sessions = await _context.PhienChats
      .Where(pc => isAdmin || pc.TinNhans.Any(tm => tm.MaNguoiGui == userId))
    .Select(pc => new
  {
           ChatSessionId = pc.MaPhienChat,
     StartTime = pc.ThoiGianBatDau,
               EndTime = pc.ThoiGianKetThuc,
             MessageCount = pc.TinNhans.Count()
    })
.ToListAsync();

     return Ok(new { success = true, data = sessions });
  }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat sessions for user {UserId}", GetCurrentUserId());
    return StatusCode(500, new { success = false, message = "Có l?i x?y ra" });
        }
        }

   private async Task<PhienChat> FindOrCreateChatSessionAsync(int userId, int recipientId)
      {
  var existingSession = await _context.PhienChats
      .Where(pc => pc.ThoiGianKetThuc == null)
           .FirstOrDefaultAsync();

   if (existingSession != null)
       return existingSession;

  var newSession = new PhienChat
            {
 ThoiGianBatDau = GetVietnamTime(),
    ThoiGianKetThuc = null
            };

  _context.PhienChats.Add(newSession);
            await _context.SaveChangesAsync();
            return newSession;
  }

        private async Task<bool> CanAccessChatSessionAsync(int userId, int chatSessionId)
        {
    if (IsAdmin()) return true;
          return await _context.TinNhans.AnyAsync(tm => tm.MaPhienChat == chatSessionId && tm.MaNguoiGui == userId);
 }
    }

    /// <summary>
    /// Request model cho realtime messaging (khác v?i SendMessageRequest trong DTOs)
    /// </summary>
    public class SendRealtimeMessageRequest
    {
        public int RecipientId { get; set; }
   public string Message { get; set; } = string.Empty;
        public int? ChatSessionId { get; set; }
    }
}