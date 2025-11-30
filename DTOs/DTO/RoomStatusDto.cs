namespace HUIT_Library.DTOs.DTO
{
    /// <summary>
    /// DTO ch?a thông tin tr?ng thái phòng hi?n t?i
    /// </summary>
    public class RoomStatusDto
    {
        /// <summary>
      /// T?ng s? phòng có s?n trong h? th?ng
        /// </summary>
        public int TongSoPhong { get; set; }

     /// <summary>
        /// S? phòng hi?n ?ang tr?ng (có th? s? d?ng)
        /// </summary>
        public int SoPhongTrong { get; set; }

        /// <summary>
        /// S? phòng hi?n ?ang b?n (?ang ???c s? d?ng)
 /// </summary>
public int SoPhongBan { get; set; }

  /// <summary>
        /// Ph?n tr?m phòng tr?ng (%)
        /// </summary>
    public double PhanTramPhongTrong { get; set; }

        /// <summary>
      /// Ph?n tr?m phòng b?n (%)
        /// </summary>
        public double PhanTramPhongBan { get; set; }

        /// <summary>
 /// Th?i gian ki?m tra
        /// </summary>
  public DateTime ThoiGianKiemTra { get; set; }

        /// <summary>
        /// Chi ti?t tr?ng thái theo t?ng lo?i phòng
    /// </summary>
        public List<RoomTypeStatusDto> ChiTietTheoLoaiPhong { get; set; } = new();
    }

    /// <summary>
    /// DTO ch?a thông tin tr?ng thái theo lo?i phòng
    /// </summary>
    public class RoomTypeStatusDto
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
        /// T?ng s? phòng lo?i này
        /// </summary>
  public int TongSo { get; set; }

   /// <summary>
    /// S? phòng tr?ng lo?i này
        /// </summary>
        public int SoPhongTrong { get; set; }

 /// <summary>
        /// S? phòng b?n lo?i này
    /// </summary>
        public int SoPhongBan { get; set; }

        /// <summary>
        /// Ph?n tr?m tr?ng c?a lo?i phòng này
    /// </summary>
        public double PhanTramTrong { get; set; }
    }
}