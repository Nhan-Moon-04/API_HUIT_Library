using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "M� ??ng nh?p l� b?t bu?c")]
        public string MaDangNhap { get; set; } = null!; // C� th? l� MaSinhVien ho?c MaNhanVien

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        public string MatKhau { get; set; } = null!;
    }
}