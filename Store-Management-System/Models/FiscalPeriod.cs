using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class FiscalPeriod
{
    public int Id { get; set; }

    public string PeriodName { get; set; } = null!;

    public string PeriodType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsClosed { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ClosedBy { get; set; }

    public int? NextPeriodId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AccountLedger> AccountLedgers { get; set; } = new List<AccountLedger>();

    public virtual ICollection<BeginningBalance> BeginningBalances { get; set; } = new List<BeginningBalance>();

    public virtual ICollection<ClosingPeriod> ClosingPeriods { get; set; } = new List<ClosingPeriod>();
}
