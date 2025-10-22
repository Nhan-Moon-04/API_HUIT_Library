using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class BotConversation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserKey { get; set; } = null!;

    public string ConversationId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual NguoiDung User { get; set; } = null!;
}
