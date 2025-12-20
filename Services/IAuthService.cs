using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Response;

namespace HUIT_Library.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> AdminLoginAsync(LoginRequest request);
        
        /// <summary>
        /// Generate JWT access token (có sessionId ?? tracking)
        /// </summary>
        string GenerateJwtToken(Models.NguoiDung user, string role, int sessionId = 0);

        Task<ForgotPasswordResponse> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<string?> GetLatestResetTokenByEmailAsync(string email);
        Task<bool> ChangePasswordAsync(string maDangNhap, string currentPassword, string newPassword);
        
        /// <summary>
        /// L?y danh sách thi?t b? ?ang ??ng nh?p c?a user
        /// </summary>
        Task<ActiveSessionsResponse> GetActiveSessionsAsync(int userId, int currentSessionId);
        
        /// <summary>
        /// ??ng xu?t m?t thi?t b? c? th?
        /// </summary>
        Task<(bool Success, string Message)> LogoutSessionAsync(int userId, int sessionId);
        
        /// <summary>
        /// ??ng xu?t t?t c? thi?t b? khác (gi? l?i thi?t b? hi?n t?i)
        /// </summary>
        Task<(bool Success, string Message)> LogoutOtherSessionsAsync(int userId, int currentSessionId);
    }
}