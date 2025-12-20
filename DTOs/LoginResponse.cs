namespace HUIT_Library.DTOs
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Token { get; set; }
        
        /// <summary>
        /// Refresh token ?? t?o access token m?i (v?nh vi?n ho?c th?i h?n dài)
        /// </summary>
        public string? RefreshToken { get; set; }
        
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int MaNguoiDung { get; set; }
        public string MaCode { get; set; } = null!;
        public string? MaSinhVien { get; set; }
        public string? MaNhanVien { get; set; }
        public string HoTen { get; set; } = null!;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string VaiTro { get; set; } = null!;
        public string? Lop { get; set; }
        public string? KhoaHoc { get; set; }
        public DateOnly? NgaySinh { get; set; }
    }
}