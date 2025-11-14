using System;
using System.Collections.Generic;

namespace HUIT_Library.Models;

public partial class VisitLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime VisitTime { get; set; }

    public virtual NguoiDung? User { get; set; }
}
