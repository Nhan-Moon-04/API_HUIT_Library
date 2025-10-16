namespace HUIT_Library.DTOs
{
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public bool EmailSent { get; set; }
        public string? Token { get; set; }
    }
}
