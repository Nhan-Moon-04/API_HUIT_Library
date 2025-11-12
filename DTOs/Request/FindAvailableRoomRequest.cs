namespace HUIT_Library.DTOs.Request
{
    /// <summary>
    /// Request DTO cho tìm ki?m phòng tr?ng theo th?i gian
  /// </summary>
    public class FindAvailableRoomRequest
    {
        /// <summary>
  /// Th?i gian b?t ??u s? d?ng phòng
        /// </summary>
        public DateTime ThoiGianBatDau { get; set; }

        /// <summary>
  /// Mã lo?i phòng (b?t bu?c)
        /// </summary>
 public int MaLoaiPhong { get; set; }

        /// <summary>
        /// Th?i gian s? d?ng (gi?) - m?c ??nh 2 gi?
      /// </summary>
 public int ThoiGianSuDung { get; set; } = 2;

     /// <summary>
      /// S?c ch?a t?i thi?u (tùy ch?n)
        /// </summary>
        public int? SucChuaToiThieu { get; set; }
    }
}