namespace HUIT_Library.DTOs.DTO;

public class ChatSessionDto
{
    public int MaPhienChat { get; set; }
    public int MaNguoiDung { get; set; }
    public int? MaNhanVien { get; set; }
    public bool? CoBot { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianKetThuc { get; set; }
    public int SoLuongTinNhan { get; set; }
    public string? TinNhanCuoi { get; set; }
    public DateTime? ThoiGianTinNhanCuoi { get; set; }
    public string LoaiPhien => CoBot == true ? "Bot Chat" : "Regular Chat";
    public bool IsActive => ThoiGianKetThuc == null;
}

public class ChatMessagesPageDto
{
    public IEnumerable<MessageDto> Messages { get; set; } = new List<MessageDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}