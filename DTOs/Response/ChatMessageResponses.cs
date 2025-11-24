namespace HUIT_Library.DTOs.Response
{
    /// <summary>
    /// DTO cho tin nh?n
    /// </summary>
    public class MessageDto
 {
      public int Id { get; set; }
      public int ChatSessionId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int? RecipientId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
 public bool IsRead { get; set; }
        public string MessageType { get; set; } = string.Empty; // "ToAdmin", "ToUser"
    }

    /// <summary>
/// DTO cho phiên chat
    /// </summary>
    public class ChatSessionDto
    {
        public int ChatSessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
      public string? LastSender { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive => EndTime == null;
        public List<ChatParticipantDto> Participants { get; set; } = new List<ChatParticipantDto>();
    }

    /// <summary>
    /// DTO cho ng??i tham gia chat
    /// </summary>
    public class ChatParticipantDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "User", "Admin"
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// DTO cho th?ng kê chat
    /// </summary>
    public class ChatStatisticsDto
    {
     public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int ActiveSessions { get; set; }
        public double AverageMessagesPerSession { get; set; }
        public List<TopUserDto> TopActiveUsers { get; set; } = new List<TopUserDto>();
 public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    /// <summary>
    /// DTO cho user ho?t ??ng nhi?u nh?t
    /// </summary>
    public class TopUserDto
  {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
  public int MessageCount { get; set; }
    }

    /// <summary>
    /// Response cho vi?c g?i tin nh?n thành công
 /// </summary>
    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MessageSentData? Data { get; set; }
    }

    /// <summary>
    /// Data cho tin nh?n v?a g?i
    /// </summary>
    public class MessageSentData
    {
        public int MessageId { get; set; }
      public int ChatSessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response cho realtime events
    /// </summary>
    public class RealtimeEventDto
    {
        public string EventType { get; set; } = string.Empty; // "MessageReceived", "UserJoined", "UserLeft", "Typing", etc.
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO cho typing indicator
 /// </summary>
    public class TypingIndicatorDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
     public bool IsTyping { get; set; }
        public int? ChatSessionId { get; set; }
    }

    /// <summary>
    /// DTO cho connection status
    /// </summary>
  public class ConnectionStatusDto
    {
  public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Online", "Offline"
  public DateTime LastActivity { get; set; }
    }

    /// <summary>
/// Response cho pagination
    /// </summary>
    public class PagedChatResponse<T>
    {
   public bool Success { get; set; }
        public List<T> Data { get; set; } = new List<T>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }

    /// <summary>
    /// Thông tin phân trang
    /// </summary>
  public class PaginationInfo
    {
   public int PageNumber { get; set; }
   public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}