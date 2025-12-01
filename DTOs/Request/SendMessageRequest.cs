namespace HUIT_Library.DTOs.Request;

public class SendMessageRequest
{
    public int MaPhienChat { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    
    /// <summary>
    /// ? Optional: Ch? c?n khi g?i t? WinForms không có JWT token
    /// N?u có JWT token, h? th?ng s? t? ??ng l?y t? token
    /// </summary>
    public int? MaNguoiDung { get; set; }
}
