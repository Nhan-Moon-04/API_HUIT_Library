namespace HUIT_Library.DTOs.Request
{
    public class CreateBookingRequest
    {
        // This represents the requested room type (MaLoaiPhong) used by the stored procedure
        public int MaLoaiPhong { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? LyDo { get; set; }

        // Optional: allow client to provide MaNguoiDung when not authenticated
        public int? MaNguoiDung { get; set; }
    }
}
