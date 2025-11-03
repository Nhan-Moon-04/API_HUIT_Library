namespace HUIT_Library.DTOs.Request
{
    public class CreateBookingRequest
    {
        /// <summary>
        /// Mã loại phòng cần đăng ký.
        /// </summary>
        public int MaLoaiPhong { get; set; }

        /// <summary>
        /// Thời gian bắt đầu mượn phòng.
        /// </summary>
        public DateTime ThoiGianBatDau { get; set; }

        /// <summary>
        /// Lý do mượn phòng (tùy chọn).
        /// </summary>
        public string? LyDo { get; set; }

        /// <summary>
        /// Ghi chú thêm cho yêu cầu (tùy chọn).
        /// </summary>
        public string? GhiChu { get; set; }

        /// <summary>
        /// Số lượng người tham gia (tùy chọn, mặc định = 1).
        /// </summary>
        public int SoLuong { get; set; } = 1;

        /// <summary>
        /// Cho phép client truyền MaNguoiDung nếu chưa xác thực qua token.
        /// </summary>
        public int? MaNguoiDung { get; set; }
    }
}
