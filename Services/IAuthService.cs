using HUIT_Library.DTOs;

namespace HUIT_Library.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> AdminLoginAsync(LoginRequest request); // For admin and staff
        string GenerateJwtToken(Models.NguoiDung user, string role);

        Task<ForgotPasswordResponse> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);

        // Development-only helper to retrieve latest reset token for a user's email
        Task<string?> GetLatestResetTokenByEmailAsync(string email);

        // Change password using current password for verification
        Task<bool> ChangePasswordAsync(string maDangNhap, string currentPassword, string newPassword);
    }
}