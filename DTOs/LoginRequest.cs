using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Mã Sinh Viên Là Bắt Buộc")]
        public string MaDangNhap { get; set; } = null!; // Có thể là MaSinhVien hoặc MaNhanVien

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string MatKhau { get; set; } = null!;
    }
}