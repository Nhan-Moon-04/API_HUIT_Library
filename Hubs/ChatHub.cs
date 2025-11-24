using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HUIT_Library.Hubs
{
    /// <summary>
    /// SignalR Hub cho Chat realtime gi?a User và Admin
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
{
        private readonly HuitThuVienContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(HuitThuVienContext context, ILogger<ChatHub> logger)
        {
         _context = context;
            _logger = logger;
     }

        /// <summary>
    /// Khi client k?t n?i
        /// </summary>
      public override async Task OnConnectedAsync()
        {
 var userId = GetCurrentUserId();
         var userName = GetCurrentUserName();
        
  _logger.LogInformation("User {UserId} ({UserName}) connected to ChatHub with ConnectionId {ConnectionId}", 
     userId, userName, Context.ConnectionId);

  // Join vào room riêng c?a user
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            
   // N?u là admin, join vào admin group
if (IsAdmin())
 {
           await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
       _logger.LogInformation("Admin {UserId} joined AdminGroup", userId);
      }

   await base.OnConnectedAsync();
        }

        /// <summary>
        /// Khi client ng?t k?t n?i
        /// </summary>
  public override async Task OnDisconnectedAsync(Exception? exception)
        {
     var userId = GetCurrentUserId();
          var userName = GetCurrentUserName();
            
  _logger.LogInformation("User {UserId} ({UserName}) disconnected from ChatHub. Exception: {Exception}", 
     userId, userName, exception?.Message);

    await base.OnDisconnectedAsync(exception);
  }

    /// <summary>
   /// G?i tin nh?n t? client
   /// </summary>
        /// <param name="recipientId">ID ng??i nh?n (0 = g?i cho admin)</param>
        /// <param name="message">N?i dung tin nh?n</param>
        /// <param name="chatSessionId">ID phiên chat (optional)</param>
      public async Task SendMessage(int recipientId, string message, int? chatSessionId = null)
        {
   try
    {
            var senderId = GetCurrentUserId();
        var senderName = GetCurrentUserName();
    
     _logger.LogInformation("User {SenderId} sending message to {RecipientId}: {Message}", 
   senderId, recipientId, message);

                // Validate input
        if (string.IsNullOrWhiteSpace(message))
                {
  await Clients.Caller.SendAsync("MessageError", "Tin nh?n không ???c ?? tr?ng");
        return;
     }

      // T?o tin nh?n m?i (logic này có th? call API /api/Message thay vì direct DB)
          var newMessage = new
        {
    Id = Guid.NewGuid().ToString(),
     SenderId = senderId,
      SenderName = senderName,
                    RecipientId = recipientId,
                    Message = message,
               Timestamp = DateTime.UtcNow,
   ChatSessionId = chatSessionId,
      MessageType = recipientId == 0 ? "ToAdmin" : "ToUser"
        };

      // G?i tin nh?n realtime
   if (recipientId == 0)
              {
         // G?i cho t?t c? admin
            await Clients.Group("AdminGroup").SendAsync("ReceiveMessage", newMessage);
   _logger.LogInformation("Message sent to AdminGroup from User {SenderId}", senderId);
   }
           else
        {
      // G?i cho user c? th?
            await Clients.Group($"User_{recipientId}").SendAsync("ReceiveMessage", newMessage);
         _logger.LogInformation("Message sent to User {RecipientId} from {SenderId}", recipientId, senderId);
         }

                // Confirm cho ng??i g?i
       await Clients.Caller.SendAsync("MessageSent", new
             {
           Success = true,
   MessageId = newMessage.Id,
            Timestamp = newMessage.Timestamp,
     Message = "Tin nh?n ?ã ???c g?i"
       });
          }
   catch (Exception ex)
            {
       _logger.LogError(ex, "Error sending message from User {SenderId} to {RecipientId}", GetCurrentUserId(), recipientId);
              await Clients.Caller.SendAsync("MessageError", "Có l?i x?y ra khi g?i tin nh?n");
       }
        }

        /// <summary>
        /// Join vào m?t phiên chat c? th?
        /// </summary>
        /// <param name="chatSessionId">ID phiên chat</param>
        public async Task JoinChatSession(int chatSessionId)
        {
    try
        {
      var userId = GetCurrentUserId();
       
          // Ki?m tra quy?n access phiên chat này
                var hasAccess = await CanAccessChatSession(userId, chatSessionId);
                if (!hasAccess)
     {
          await Clients.Caller.SendAsync("JoinChatSessionError", "B?n không có quy?n truy c?p phiên chat này");
   return;
      }

    await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatSession_{chatSessionId}");
            await Clients.Caller.SendAsync("JoinedChatSession", new
  {
      ChatSessionId = chatSessionId,
       Message = "?ã tham gia phiên chat"
            });

          _logger.LogInformation("User {UserId} joined ChatSession {ChatSessionId}", userId, chatSessionId);
            }
            catch (Exception ex)
            {
        _logger.LogError(ex, "Error joining chat session {ChatSessionId} for user {UserId}", chatSessionId, GetCurrentUserId());
      await Clients.Caller.SendAsync("JoinChatSessionError", "Có l?i x?y ra khi tham gia phiên chat");
     }
        }

        /// <summary>
        /// R?i kh?i phiên chat
     /// </summary>
        /// <param name="chatSessionId">ID phiên chat</param>
        public async Task LeaveChatSession(int chatSessionId)
        {
 try
            {
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatSession_{chatSessionId}");
   await Clients.Caller.SendAsync("LeftChatSession", new
    {
        ChatSessionId = chatSessionId,
 Message = "?ã r?i kh?i phiên chat"
                });

    _logger.LogInformation("User {UserId} left ChatSession {ChatSessionId}", GetCurrentUserId(), chatSessionId);
 }
            catch (Exception ex)
        {
    _logger.LogError(ex, "Error leaving chat session {ChatSessionId} for user {UserId}", chatSessionId, GetCurrentUserId());
            }
        }

        /// <summary>
      /// ?ánh d?u tin nh?n ?ã ??c
        /// </summary>
        /// <param name="messageId">ID tin nh?n</param>
        public async Task MarkMessageAsRead(string messageId)
      {
            try
        {
 var userId = GetCurrentUserId();
           
    // Notify v? vi?c ?ã ??c tin nh?n (có th? implement logic mark as read ? DB)
           await Clients.Others.SendAsync("MessageRead", new
          {
        MessageId = messageId,
     ReadBy = userId,
   ReadAt = DateTime.UtcNow
        });

                _logger.LogInformation("User {UserId} marked message {MessageId} as read", userId, messageId);
            }
            catch (Exception ex)
            {
      _logger.LogError(ex, "Error marking message {MessageId} as read by user {UserId}", messageId, GetCurrentUserId());
     }
        }

    /// <summary>
        /// Thông báo user ?ang typing
        /// </summary>
        /// <param name="recipientId">ID ng??i nh?n</param>
        public async Task StartTyping(int recipientId)
        {
            try
            {
   var userId = GetCurrentUserId();
       var userName = GetCurrentUserName();

        if (recipientId == 0)
           {
        // Notify admin
   await Clients.Group("AdminGroup").SendAsync("UserStartTyping", new
             {
        UserId = userId,
   UserName = userName
           });
           }
     else
                {
      // Notify specific user
    await Clients.Group($"User_{recipientId}").SendAsync("UserStartTyping", new
  {
          UserId = userId,
     UserName = userName
     });
        }
            }
       catch (Exception ex)
    {
              _logger.LogError(ex, "Error sending typing indicator from {UserId} to {RecipientId}", GetCurrentUserId(), recipientId);
   }
   }

 /// <summary>
     /// Thông báo user ng?ng typing
      /// </summary>
        /// <param name="recipientId">ID ng??i nh?n</param>
        public async Task StopTyping(int recipientId)
        {
  try
            {
                var userId = GetCurrentUserId();

      if (recipientId == 0)
      {
   await Clients.Group("AdminGroup").SendAsync("UserStopTyping", new { UserId = userId });
                }
             else
                {
          await Clients.Group($"User_{recipientId}").SendAsync("UserStopTyping", new { UserId = userId });
         }
       }
            catch (Exception ex)
          {
    _logger.LogError(ex, "Error sending stop typing indicator from {UserId} to {RecipientId}", GetCurrentUserId(), recipientId);
            }
        }

        // Helper methods
        private int GetCurrentUserId()
     {
   var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       return int.TryParse(userIdClaim, out var userId) ? userId : 0;
  }

        private string GetCurrentUserName()
    {
 return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        }

        private bool IsAdmin()
        {
            return Context.User?.IsInRole("Admin") == true || 
            Context.User?.IsInRole("NhanVien") == true;
        }

        private async Task<bool> CanAccessChatSession(int userId, int chatSessionId)
        {
        try
    {
    // Check if user is participant in this chat session
            var session = await _context.PhienChats.FindAsync(chatSessionId);
if (session == null) return false;

       // Admin can access all sessions
      if (IsAdmin()) return true;

   // Check if user has messages in this session (simplified check)
       var hasMessages = await _context.TinNhans
    .AnyAsync(tm => tm.MaPhienChat == chatSessionId);

    return hasMessages;
 }
 catch (Exception ex)
 {
       _logger.LogError(ex, "Error checking access for user {UserId} to chat session {ChatSessionId}", userId, chatSessionId);
    return false;
      }
        }
    }
}