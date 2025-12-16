using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.DTOs.Response
{
    public class UpdateProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public UserProfileDto? Data { get; set; }

        public static UpdateProfileResponse SuccessResult(UserProfileDto data, string message = "C?p nh?t thông tin thành công")
        {
            return new UpdateProfileResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static UpdateProfileResponse ErrorResult(string message, Dictionary<string, List<string>>? errors = null)
        {
            return new UpdateProfileResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new Dictionary<string, List<string>>()
            };
        }

        public void AddError(string field, string errorMessage)
        {
            if (!Errors.ContainsKey(field))
            {
                Errors[field] = new List<string>();
            }
            Errors[field].Add(errorMessage);
        }
    }
}