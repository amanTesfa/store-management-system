using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ClosedRelation
{
    public int Id { get; set; }

    public int OriginalVoucherId { get; set; }

    public int ClosingVoucherId { get; set; }

    public string RelationType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime ClosedAt { get; set; }

    public int ClosedBy { get; set; }

    public bool IsApproved { get; set; }

    public int? ApprovedBy { get; set; }

    public virtual Voucher ClosingVoucher { get; set; } = null!;

    public virtual Voucher OriginalVoucher { get; set; } = null!;
}
