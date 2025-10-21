using System;

namespace HUIT_Library.DTOs.DTO;

public class MessageDto
{
    public int MaTinNhan { get; set; }
    public int MaPhienChat { get; set; }
    public int MaNguoiGui { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public DateTime? ThoiGianGui { get; set; }
    public bool? LaBot { get; set; }
}
