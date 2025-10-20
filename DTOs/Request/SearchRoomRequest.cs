namespace HUIT_Library.DTOs.Request
{
    public class SearchRoomRequest
    {
        public DateTime? Date { get; set; }
        // Accept time values as strings from clients (empty string or null allowed)
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int? MinimumCapacity { get; set; }
        public string? Keyword { get; set; }
    }
}
