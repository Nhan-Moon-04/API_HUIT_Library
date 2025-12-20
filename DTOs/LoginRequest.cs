using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Mã Sinh Viên Là Bắt Buộc")]
        public string MaDangNhap { get; set; } = null!; // Có thể là MaSinhVien hoặc MaNhanVien

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string MatKhau { get; set; } = null!;

        /// <summary>
        /// Thông tin thiết bị (Browser, OS, Device Name)
        /// ⚠️ Tự động detect từ User-Agent header nếu không cung cấp
        /// VD: "Chrome 120.0 on Windows 10", "Mobile Safari on iPhone 14"
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Địa chỉ IP của thiết bị đăng nhập
        /// ⚠️ Tự động detect từ HTTP Connection nếu không cung cấp
        /// </summary>
        public string? IpAddress { get; set; }
    }
}