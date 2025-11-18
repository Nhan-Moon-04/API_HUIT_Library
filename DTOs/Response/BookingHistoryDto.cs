namespace HUIT_Library.DTOs.Response
{
public class BookingHistoryDto
    {
        public int MaDangKy { get; set; }
        public int MaPhong { get; set; }
        public string? TenPhong { get; set; }
        public string? TenLoaiPhong { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public DateTime? GioBatDauThucTe { get; set; }
        public DateTime? GioKetThucThucTe { get; set; }
        public string? LyDo { get; set; }
        public int? SoLuong { get; set; }
        public string? GhiChu { get; set; }
        public int MaTrangThai { get; set; }
        public string? TenTrangThai { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateOnly? NgayMuon { get; set; }
        public string? TinhTrangPhong { get; set; }
        public string? GhiChuSuDung { get; set; }
        public bool CoBienBan { get; set; }
        public int SoLuongBienBan { get; set; }
        public List<ViolationSummaryDto>? DanhSachViPham { get; set; }

    // ? Thêm thông tin ?ánh giá
        public bool DaDanhGia { get; set; }
        public int? MaDanhGia { get; set; }
        public int? DiemDanhGia { get; set; }
        public bool CoTheDanhGia { get; set; }
        public string? TrangThaiDanhGia { get; set; } // "?ánh giá ngay", "Xem ?ánh giá", "H?t h?n ?ánh giá"
        public int? SoNgayConLaiDeDanhGia { get; set; }
    }

    public class ViolationSummaryDto
    {
        public int MaViPham { get; set; }
        public string? TenViPham { get; set; }
        public DateTime? NgayLap { get; set; }
        public string? TrangThaiXuLy { get; set; }
    }
}