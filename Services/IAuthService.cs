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

    }
}