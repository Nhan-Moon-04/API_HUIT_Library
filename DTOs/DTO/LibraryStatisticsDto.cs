using HUIT_Library.DTOs.DTO;

namespace HUIT_Library.DTOs.DTO
{
    /// <summary>
    /// DTO cho thống kê trang web thư viện
    /// </summary>
    public class LibraryStatisticsDto
    {
        /// <summary>
        /// Tổng lượt truy cập
        /// </summary>
        public long TongLuotTruyCap { get; set; }

        /// <summary>
        /// Số lượng người dùng online hiện tại
        /// </summary>
        public int SoLuongOnline { get; set; }

        /// <summary>
        /// Thành viên đang online
        /// </summary>
        public int ThanhVienOnline { get; set; }

        /// <summary>
        /// Khách đang online (chưa đăng nhập)
        /// </summary>
        public int KhachOnline { get; set; }

        /// <summary>
        /// Lượt truy cập trong ngày
        /// </summary>
        public int TrongNgay { get; set; }

        /// <summary>
        /// Lượt truy cập hôm qua
        /// </summary>
        public int HomQua { get; set; }

        /// <summary>
        /// Lượt truy cập trong tháng
        /// </summary>
        public int TrongThang { get; set; }
    }
}