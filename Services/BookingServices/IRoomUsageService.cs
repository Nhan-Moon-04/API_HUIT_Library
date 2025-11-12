namespace HUIT_Library.Services.BookingServices
{
    /// <summary>
    /// Interface cho quản lý trạng thái sử dụng phòng
    /// </summary>
    public interface IRoomUsageService
    {
        /// <summary>
        /// Bắt đầu sử dụng phòng (check-in)
        /// </summary>
     Task<(bool Success, string? Message)> StartRoomUsageAsync(int userId, int maDangKy);

        /// <summary>
        /// Lấy trạng thái sử dụng phòng hiện tại
        /// </summary>
  Task<RoomUsageStatusDto?> GetRoomUsageStatusAsync(int userId, int maDangKy);

        /// <summary>
        /// Cập nhật tình trạng phòng khi trả
        /// </summary>
        Task<(bool Success, string? Message)> UpdateRoomConditionAsync(int userId, int maDangKy, string tinhTrangPhong, string? ghiChu = null);

        /// <summary>
        /// Lấy lịch sử sử dụng phòng
    /// </summary>
        Task<List<RoomUsageHistoryDto>> GetRoomUsageHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10);
    }

    /// <summary>
    /// DTO cho trạng thái sử dụng phòng
    /// </summary>
    public class RoomUsageStatusDto
    {
   public int MaDangKy { get; set; }
        public int MaPhong { get; set; }
   public string? TenPhong { get; set; }
        public DateTime? GioBatDauThucTe { get; set; }
        public DateTime ThoiGianKetThucDuKien { get; set; }
      public int MinutesRemaining { get; set; }
        public string? TinhTrangPhong { get; set; }
        public bool CanExtend { get; set; }
        public bool CanComplete { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho lịch sử sử dụng phòng
    /// </summary>
    public class RoomUsageHistoryDto
    {
    public int MaDangKy { get; set; }
        public int MaPhong { get; set; }
        public string? TenPhong { get; set; }
        public string? TenLoaiPhong { get; set; }
        public DateTime ThoiGianDangKy { get; set; }
        public DateTime ThoiGianKetThucDangKy { get; set; }
        public DateTime? GioBatDauThucTe { get; set; }
        public DateTime? GioKetThucThucTe { get; set; }
        public TimeSpan? ThoiGianSuDungThucTe { get; set; }
        public string? TinhTrangPhong { get; set; }
        public string? GhiChu { get; set; }
   public bool HasViolation { get; set; }
    }
}