namespace HUIT_Library.DTOs.DTO
{
    public class NotificationDto
    {
        public int MaThongBao { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public bool DaDoc { get; set; }
    }

    public class NotificationDetailsDto
    {
        public int MaThongBao { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public bool DaDoc { get; set; }
        public string? GhiChu { get; set; }
    }
}
