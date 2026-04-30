using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TransactionReference
{
    public int Id { get; set; }

    public int SourceVoucherId { get; set; }

    public int TargetVoucherId { get; set; }

    public string ReferenceType { get; set; } = null!;

    public DateTime ReferenceDate { get; set; }

    public string? Notes { get; set; }

    public virtual Voucher SourceVoucher { get; set; } = null!;

    public virtual Voucher TargetVoucher { get; set; } = null!;
}
