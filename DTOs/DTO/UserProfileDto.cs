namespace HUIT_Library.DTOs.DTO
{
    public class UserProfileDto
    {
        public int MaNguoiDung { get; set; }

        public string MaDangNhap { get; set; } = null!;

        public string MatKhau { get; set;  } = null!;

        public int MaVaiTro { get; set; }

        public string? HoTen { get; set; }

        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }
    }
}
