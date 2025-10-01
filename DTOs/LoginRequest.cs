using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Mã ??ng nh?p là b?t bu?c")]
        public string MaDangNhap { get; set; } = null!; // Có th? là MaSinhVien ho?c MaNhanVien

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        public string MatKhau { get; set; } = null!;
    }
}