namespace HUIT_Library.DTOs.Request
{
  /// <summary>
    /// Request DTO cho vi?c h?y ??ng ký phòng v?i lý do
    /// </summary>
    public class CancelBookingRequest
  {
      /// <summary>
        /// Mã ??ng ký c?n h?y
  /// </summary>
        public int MaDangKy { get; set; }

        /// <summary>
        /// Lý do h?y ??ng ký (b?t bu?c)
  /// </summary>
        public string LyDoHuy { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú thêm (tùy ch?n)
        /// </summary>
     public string? GhiChu { get; set; }
 }
}