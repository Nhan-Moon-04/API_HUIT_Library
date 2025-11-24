namespace HUIT_Library.DTOs.Request
{
    /// <summary>
    /// Request model cho tham gia phiên chat
    /// </summary>
    public class JoinChatSessionRequest
    {
        /// <summary>
        /// ID phiên chat mu?n tham gia
        /// </summary>
        public int ChatSessionId { get; set; }
    }

    /// <summary>
  /// Request model cho ?ánh d?u tin nh?n ?ã ??c
    /// </summary>
    public class MarkMessageReadRequest
    {
        /// <summary>
  /// ID tin nh?n c?n ?ánh d?u ?ã ??c
        /// </summary>
        public int MessageId { get; set; }
    }

    /// <summary>
    /// Request model cho typing indicator
    /// </summary>
    public class TypingIndicatorRequest
    {
        /// <summary>
      /// ID ng??i nh?n (0 = admin)
    /// </summary>
        public int RecipientId { get; set; }

     /// <summary>
        /// True = ?ang typing, False = ng?ng typing
 /// </summary>
   public bool IsTyping { get; set; }
    }
}