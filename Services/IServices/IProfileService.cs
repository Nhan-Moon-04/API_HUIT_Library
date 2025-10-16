using Dapper;
using HUIT_Library.DTOs.DTO;
using Microsoft.EntityFrameworkCore;
using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;

namespace HUIT_Library.Services.IServices
{
    public interface IProfileService
    {

        Task<UserProfileDto?> GetUserProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
    }
}
