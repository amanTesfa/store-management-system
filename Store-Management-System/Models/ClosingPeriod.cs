using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ClosingPeriod
{
    public int Id { get; set; }

    public int FiscalPeriodId { get; set; }

    public string ClosingType { get; set; } = null!;

    public decimal StockClosingValue { get; set; }

    public DateOnly ClosingDate { get; set; }

    public bool IsReopened { get; set; }

    public DateTime? ReopenedAt { get; set; }

    public int? ReopenedBy { get; set; }

    public DateTime ClosedAt { get; set; }

    public int ClosedBy { get; set; }

    public virtual FiscalPeriod FiscalPeriod { get; set; } = null!;
}
