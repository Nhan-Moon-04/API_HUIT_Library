using System.ComponentModel.DataAnnotations;

namespace HUIT_Library.DTOs
{
    public class RegisterRequest : IValidatableObject
    {
        [Required(ErrorMessage = "H? t�n l� b?t bu?c")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng ?�ng ??nh d?ng")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        [MinLength(6, ErrorMessage = "M?t kh?u ph?i c� �t nh?t 6 k� t?")]
        public string MatKhau { get; set; } = null!;

        [Phone(ErrorMessage = "S? ?i?n tho?i kh�ng ?�ng ??nh d?ng")]
        public string? SoDienThoai { get; set; }

        // Cho sinh vi�n
        public string? MaSinhVien { get; set; }
        public string? Lop { get; set; }
        public string? KhoaHoc { get; set; }

        // Cho gi?ng vi�n
        public string? MaNhanVien { get; set; }

        public DateOnly? NgaySinh { get; set; }

        [Required(ErrorMessage = "Vai tr� l� b?t bu?c")]
        public string VaiTro { get; set; } = null!; // "SINH_VIEN" ho?c "GIANG_VIEN"

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (VaiTro == "SINH_VIEN")
            {
                if (string.IsNullOrEmpty(MaSinhVien))
                {
                    yield return new ValidationResult(
                        "M� sinh vi�n l� b?t bu?c khi vai tr� l� sinh vi�n", 
                        new[] { nameof(MaSinhVien) });
                }
                
                if (string.IsNullOrEmpty(Lop))
                {
                    yield return new ValidationResult(
                        "L?p l� b?t bu?c khi vai tr� l� sinh vi�n", 
                        new[] { nameof(Lop) });
                }
                
                if (string.IsNullOrEmpty(KhoaHoc))
                {
                    yield return new ValidationResult(
                        "Kh�a h?c l� b?t bu?c khi vai tr� l� sinh vi�n", 
                        new[] { nameof(KhoaHoc) });
                }
            }
            else if (VaiTro == "GIANG_VIEN")
            {
                if (string.IsNullOrEmpty(MaNhanVien))
                {
                    yield return new ValidationResult(
                        "M� nh�n vi�n l� b?t bu?c khi vai tr� l� gi?ng vi�n", 
                        new[] { nameof(MaNhanVien) });
                }
            }
            else if (!string.IsNullOrEmpty(VaiTro))
            {
                yield return new ValidationResult(
                    "Vai tr� ch? ???c ph�p l� 'SINH_VIEN' ho?c 'GIANG_VIEN'", 
                    new[] { nameof(VaiTro) });
            }
        }
    }
}