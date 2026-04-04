using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class BeginningBalance
{
    public int Id { get; set; }

    public string BalanceNumber { get; set; } = null!;

    public DateOnly BalanceDate { get; set; }

    public int FiscalPeriodId { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PostedAt { get; set; }

    public int? PostedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual ICollection<BeginningBalanceLine> BeginningBalanceLines { get; set; } = new List<BeginningBalanceLine>();

    public virtual FiscalPeriod FiscalPeriod { get; set; } = null!;
}
