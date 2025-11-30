namespace HUIT_Library.DTOs.DTO
{
    /// <summary>
    /// DTO ch?a thông tin gi?i h?n s? l??ng ng??i cho t?ng lo?i phòng
    /// </summary>
    public class RoomCapacityLimitsDto
    {
        /// <summary>
        /// Mã lo?i phòng
  /// </summary>
        public int MaLoaiPhong { get; set; }
        
    /// <summary>
/// Tên lo?i phòng
  /// </summary>
        public string TenLoaiPhong { get; set; } = string.Empty;
        
  /// <summary>
      /// S?c ch?a t?i ?a c?a phòng (t? database)
        /// </summary>
 public int SucChuaToiDa { get; set; }
        
        /// <summary>
        /// S? l??ng t?i thi?u ???c phép ??ng ký
      /// </summary>
        public int SoLuongToiThieu { get; set; }
        
        /// <summary>
        /// S? l??ng t?i ?a ???c phép ??ng ký
/// </summary>
     public int SoLuongToiDa { get; set; }
        
        /// <summary>
        /// Mô t? quy ??nh v? s? l??ng
    /// </summary>
public string MoTa { get; set; } = string.Empty;
    }
}