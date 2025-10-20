namespace HUIT_Library.DTOs.DTO
{
    public class RoomDto
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public int? SucChua { get; set; }
        public string? TinhTrang { get; set; }
    }

    public class RoomDetailsDto
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public int? SucChua { get; set; }
        public string? TinhTrang { get; set; }
        public IEnumerable<ResourceDto> TaiNguyen { get; set; } = new List<ResourceDto>();
        public IEnumerable<RoomScheduleDto> LichSu { get; set; } = new List<RoomScheduleDto>();
    }

    public class ResourceDto
    {
        public int MaTaiNguyen { get; set; }
        public string TenTaiNguyen { get; set; } = string.Empty;
        public int? SoLuong { get; set; }
        public string? TinhTrang { get; set; }
    }

    public class RoomScheduleDto
    {
        public int MaLich { get; set; }
        public DateOnly Ngay { get; set; }
        public TimeOnly GioBatDau { get; set; }
        public TimeOnly GioKetThuc { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }
}
