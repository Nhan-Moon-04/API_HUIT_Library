namespace HUIT_Library.DTOs.Request
{
    public class SubmitRatingRequest
    {
        // Identifiers
        public int MaDangKy { get; set; } // booking id (optional association for service/staff)
        public int MaPhong { get; set; }   // room id being reviewed
        public int? MaNhanVien { get; set; } // optional staff id being reviewed

        // Scores 1..5. If null, that criterion will be skipped.
        public int? RoomScore { get; set; }
        public int? ServiceScore { get; set; }
        public int? StaffScore { get; set; }

        // Optional textual comment for all ratings
        public string? NoiDung { get; set; }
    }
}