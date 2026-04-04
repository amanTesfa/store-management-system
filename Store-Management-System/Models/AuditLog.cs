using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class AuditLog
{
    public int Id { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string Action { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedAt { get; set; }

    public int ChangedBy { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
