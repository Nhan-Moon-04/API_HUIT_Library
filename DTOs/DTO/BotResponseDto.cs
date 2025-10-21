namespace HUIT_Library.DTOs.DTO;

public class BotResponseDto
{
    public MessageDto? BotMessage { get; set; }
    public MessageDto? UserMessage { get; set; }
    public bool RequiresStaff { get; set; }
    public string? StaffRequestReason { get; set; }
}