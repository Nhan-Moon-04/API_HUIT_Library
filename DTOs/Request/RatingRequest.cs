namespace HUIT_Library.DTOs.Request
{
    /// <summary>
    /// Request ?? t?o ?ánh giá phòng
    /// </summary>
    public class CreateRatingRequest
    {
        /// <summary>
        /// Lo?i ??i t??ng ?ánh giá - hi?n t?i ch? h? tr? "PHONG"
        /// </summary>
        public string LoaiDoiTuong { get; set; } = "PHONG";

        /// <summary>
        /// Mã phòng ???c ?ánh giá
        /// </summary>
        public int MaDoiTuong { get; set; }

        /// <summary>
        /// ?i?m ?ánh giá t? 1-5 sao
        /// </summary>
        public int DiemDanhGia { get; set; }

        /// <summary>
        /// N?i dung ?ánh giá (tùy ch?n)
        /// </summary>
        public string? NoiDung { get; set; }

        /// <summary>
        /// Mã ??ng ký phòng (b?t bu?c ?? xác th?c user ?ã s? d?ng phòng)
        /// </summary>
        public int? MaDangKy { get; set; }
    }

    /// <summary>
    /// Request ?? c?p nh?t ?ánh giá - ch? trong 1 tu?n sau khi tr? phòng
    /// </summary>
    public class UpdateRatingRequest
    {
        /// <summary>
        /// ?i?m ?ánh giá t? 1-5 sao
        /// </summary>
        public int DiemDanhGia { get; set; }

        /// <summary>
        /// N?i dung ?ánh giá (tùy ch?n)
        /// </summary>
        public string? NoiDung { get; set; }
    }

    /// <summary>
    /// Request ?? l?c ?ánh giá phòng
    /// </summary>
    public class RatingFilterRequest
    {
        /// <summary>
        /// Lo?i ??i t??ng - hi?n t?i ch? h? tr? "PHONG"
        /// </summary>
        public string? LoaiDoiTuong { get; set; } = "PHONG";

        /// <summary>
        /// Mã phòng c? th?
        /// </summary>
        public int? MaDoiTuong { get; set; }

        /// <summary>
        /// ?i?m ?ánh giá t?i thi?u
        /// </summary>
        public int? DiemToiThieu { get; set; }

        /// <summary>
        /// ?i?m ?ánh giá t?i ?a
        /// </summary>
        public int? DiemToiDa { get; set; }

        /// <summary>
        /// T? ngày
        /// </summary>
        public DateTime? TuNgay { get; set; }

        /// <summary>
        /// ??n ngày
        /// </summary>
        public DateTime? DenNgay { get; set; }
    }
}