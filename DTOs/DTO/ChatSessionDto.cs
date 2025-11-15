namespace HUIT_Library.DTOs.DTO;

public class ChatSessionDto
{
    public int MaPhienChat { get; set; }
    public int MaNguoiDung { get; set; }
    public int? MaNhanVien { get; set; }
    public bool? CoBot { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianKetThuc { get; set; }
    public int SoLuongTinNhan { get; set; }
    public string? TinNhanCuoi { get; set; }
    public DateTime? ThoiGianTinNhanCuoi { get; set; }
    public string LoaiPhien => CoBot == true ? "Bot Chat" : "Regular Chat";
    public bool IsActive => ThoiGianKetThuc == null;
}

public class ChatMessagesPageDto
{
    public IEnumerable<MessageDto> Messages { get; set; } = new List<MessageDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class UserChatHistoryDto
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public DateTime? FirstChatDate { get; set; }
    public DateTime? LastChatDate { get; set; }
    public IEnumerable<ChatSessionDto> Sessions { get; set; } = new List<ChatSessionDto>();
    public ChatStatisticsDto Statistics { get; set; } = new ChatStatisticsDto();
}

public class ChatStatisticsDto
{
    public int BotSessions { get; set; }
    public int RegularSessions { get; set; }
    public int StaffSessions { get; set; }
    public int TotalBotMessages { get; set; }
    public int TotalUserMessages { get; set; }
    public int ActiveSessions { get; set; }
    public int CompletedSessions { get; set; }
}

public class ChatSessionWithMessagesDto
{
    public ChatSessionDto SessionInfo { get; set; } = new ChatSessionDto();
    public IEnumerable<MessageDto> Messages { get; set; } = new List<MessageDto>();
    public int TotalMessages { get; set; }
}

public class ChatSessionStaffDto
{
    public int MaPhienChat { get; set; }
    public int MaNguoiDung { get; set; }
    public int MaNhanVien { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianKetThuc { get; set; }
    public int SoLuongTinNhan { get; set; }
    public string? TinNhanCuoi { get; set; }
    public DateTime? ThoiGianTinNhanCuoi { get; set; }
}