namespace HUIT_Library.DTOs.Response
{
    /// <summary>
    /// DTO cho thông tin phiên ??ng nh?p (thi?t b?)
    /// </summary>
    public class UserSessionDto
    {
        public int SessionId { get; set; }
        
        /// <summary>
        /// Thông tin thi?t b? (Browser, OS, Device Name)
        /// </summary>
        public string? DeviceInfo { get; set; }
        
        /// <summary>
        /// ??a ch? IP khi ??ng nh?p
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// Th?i gian ??ng nh?p
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Th?i gian truy c?p cu?i cùng
        /// </summary>
        public DateTime? LastAccessAt { get; set; }
        
        /// <summary>
        /// Th?i gian h?t h?n (null = v?nh vi?n)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Phiên hi?n t?i (phiên ?ang dùng ?? g?i API)
        /// </summary>
        public bool IsCurrentSession { get; set; }
        
        /// <summary>
        /// Tr?ng thái phiên
        /// </summary>
        public string Status { get; set; } = "Active";
    }

    /// <summary>
    /// Response danh sách thi?t b? ??ng nh?p
    /// </summary>
    public class ActiveSessionsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// T?ng s? thi?t b? ?ang ho?t ??ng
        /// </summary>
        public int TotalActiveSessions { get; set; }
        
        /// <summary>
        /// S? thi?t b? t?i ?a cho phép
        /// </summary>
        public int MaxAllowedSessions { get; set; }
        
        /// <summary>
        /// ? Thông tin thi?t b? hi?n t?i ?ang g?i API
        /// </summary>
        public UserSessionDto? CurrentDevice { get; set; }
        
        /// <summary>
        /// Danh sách thi?t b?
        /// </summary>
        public List<UserSessionDto> Sessions { get; set; } = new();
    }
}
