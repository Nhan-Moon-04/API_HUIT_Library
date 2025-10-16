using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class PasswordResetToken
{
    public int Id { get; set; }

    public int MaNguoiDung { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpireAt { get; set; }

    public bool? Used { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
