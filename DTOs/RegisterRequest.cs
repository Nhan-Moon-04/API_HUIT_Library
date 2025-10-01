using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class RegisterRequest : IValidatableObject
    {
        [Required(ErrorMessage = "H? tên là b?t bu?c")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Email là b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email không ?úng ??nh d?ng")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [MinLength(6, ErrorMessage = "M?t kh?u ph?i có ít nh?t 6 ký t?")]
        public string MatKhau { get; set; } = null!;

        [Phone(ErrorMessage = "S? ?i?n tho?i không ?úng ??nh d?ng")]
        public string? SoDienThoai { get; set; }

        // Cho sinh viên
        public string? MaSinhVien { get; set; }
        public string? Lop { get; set; }
        public string? KhoaHoc { get; set; }

        // Cho gi?ng viên
        public string? MaNhanVien { get; set; }

        public DateOnly? NgaySinh { get; set; }

        [Required(ErrorMessage = "Vai trò là b?t bu?c")]
        public string VaiTro { get; set; } = null!; // "SINH_VIEN" ho?c "GIANG_VIEN"

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (VaiTro == "SINH_VIEN")
            {
                if (string.IsNullOrEmpty(MaSinhVien))
                {
                    yield return new ValidationResult(
                        "Mã sinh viên là b?t bu?c khi vai trò là sinh viên", 
                        new[] { nameof(MaSinhVien) });
                }
                
                if (string.IsNullOrEmpty(Lop))
                {
                    yield return new ValidationResult(
                        "L?p là b?t bu?c khi vai trò là sinh viên", 
                        new[] { nameof(Lop) });
                }
                
                if (string.IsNullOrEmpty(KhoaHoc))
                {
                    yield return new ValidationResult(
                        "Khóa h?c là b?t bu?c khi vai trò là sinh viên", 
                        new[] { nameof(KhoaHoc) });
                }
            }
            else if (VaiTro == "GIANG_VIEN")
            {
                if (string.IsNullOrEmpty(MaNhanVien))
                {
                    yield return new ValidationResult(
                        "Mã nhân viên là b?t bu?c khi vai trò là gi?ng viên", 
                        new[] { nameof(MaNhanVien) });
                }
            }
            else if (!string.IsNullOrEmpty(VaiTro))
            {
                yield return new ValidationResult(
                    "Vai trò ch? ???c phép là 'SINH_VIEN' ho?c 'GIANG_VIEN'", 
                    new[] { nameof(VaiTro) });
            }
        }
    }
}