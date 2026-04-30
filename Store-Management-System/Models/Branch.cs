using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Branch
{
    public int Id { get; set; }

    public string BranchCode { get; set; } = null!;

    public string BranchName { get; set; } = null!;

    public string BranchType { get; set; } = null!;

    public int? ParentBranchId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ManagerName { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? OpeningDate { get; set; }

    public string? TimeZone { get; set; }
}
