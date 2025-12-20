using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class UserSession
{
    public int Id { get; set; }

    public int MaNguoiDung { get; set; }

    public string RefreshToken { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastAccessAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? RevokeReason { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
