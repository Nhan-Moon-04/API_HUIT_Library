using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;

namespace HUIT_Library.Services.IServices
{
    /// <summary>
 /// Interface cho d?ch v? qu?n lý ?ánh giá phòng
 /// </summary>
    public interface IRatingService
    {
        /// <summary>
  /// T?o ?ánh giá phòng m?i (ch? trong vòng 1 tu?n sau khi tr? phòng)
        /// </summary>
    Task<(bool Success, string Message, int? MaDanhGia)> CreateRatingAsync(int userId, CreateRatingRequest request);

        /// <summary>
     /// C?p nh?t ?ánh giá phòng (ch? trong 1 tu?n sau khi tr? phòng)
        /// </summary>
        Task<(bool Success, string Message)> UpdateRatingAsync(int userId, int maDanhGia, UpdateRatingRequest request);

      /// <summary>
       /// L?y ?ánh giá phòng c?a user
        /// </summary>
        Task<List<RatingDto>> GetUserRatingsAsync(int userId, int pageNumber = 1, int pageSize = 10);

     /// <summary>
        /// L?y ?ánh giá theo phòng
        /// </summary>
        Task<PagedRatingResponse> GetRatingsByObjectAsync(string loaiDoiTuong, int maDoiTuong, int pageNumber = 1, int pageSize = 10);

/// <summary>
      /// L?y th?ng kê ?ánh giá theo phòng
        /// </summary>
  Task<RatingStatisticsDto?> GetRatingStatisticsAsync(string loaiDoiTuong, int maDoiTuong);

        /// <summary>
        /// Tìm ki?m ?ánh giá phòng
        /// </summary>
  Task<PagedRatingResponse> SearchRatingsAsync(RatingFilterRequest filter, int pageNumber = 1, int pageSize = 10);

/// <summary>
      /// L?y chi ti?t m?t ?ánh giá
        /// </summary>
        Task<RatingDetailDto?> GetRatingDetailAsync(int maDanhGia);

/// <summary>
     /// Ki?m tra user có th? ?ánh giá phòng này không (trong vòng 1 tu?n sau khi tr? phòng)
        /// </summary>
        Task<(bool CanRate, string Message)> CanUserRateAsync(int userId, string loaiDoiTuong, int maDoiTuong, int? maDangKy = null);
    }
}