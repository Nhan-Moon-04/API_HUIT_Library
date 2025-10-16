namespace HUIT_Library.DTOs.Request
{
    public class ChangePasswordRequest
    {
        public string MaCode { get; set; } = null!;
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}