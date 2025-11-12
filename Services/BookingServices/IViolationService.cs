namespace HUIT_Library.Services.BookingServices
{
    /// <summary>
    /// Interface cho quản lý vi phạm
    /// </summary>
    public interface IViolationService
    {
/// <summary>
        /// Lấy danh sách vi phạm của user
     /// </summary>
        Task<List<ViolationDto>> GetUserViolationsAsync(int userId, int pageNumber = 1, int pageSize = 10);

      /// <summary>
        /// Kiểm tra user có vi phạm gần đây không
      /// </summary>
        Task<(bool HasViolations, int ViolationCount, string Message)> CheckRecentViolationsAsync(int userId, int monthsBack = 6);

        /// <summary>
        /// Lấy chi tiết vi phạm
        /// </summary>
        Task<ViolationDetailDto?> GetViolationDetailAsync(int userId, int maViPham);
    }

    /// <summary>
    /// DTO cho thông tin vi phạm
/// </summary>
    public class ViolationDto
 {
        public int MaViPham { get; set; }
        public string? TenViPham { get; set; }
        public string? MoTa { get; set; }
        public DateTime NgayLap { get; set; }
        public string? TrangThaiXuLy { get; set; }
        public int MaDangKy { get; set; }
    public string? TenPhong { get; set; }
        public DateTime? ThoiGianViPham { get; set; }
    }

 /// <summary>
    /// DTO cho chi tiết vi phạm
    /// </summary>
    public class ViolationDetailDto : ViolationDto
    {
 public string? GhiChu { get; set; }
        public string? NguoiLapBienBan { get; set; }
    }
}