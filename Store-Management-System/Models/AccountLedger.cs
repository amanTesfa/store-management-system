using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class AccountLedger
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public DateOnly PostingDate { get; set; }

    public int FiscalPeriodId { get; set; }

    public string? Reference { get; set; }

    public virtual FiscalPeriod FiscalPeriod { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
