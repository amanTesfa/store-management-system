using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ChargeAllocation
{
    public int Id { get; set; }

    public int VoucherChargeId { get; set; }

    public int VoucherLineId { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string AllocationMethod { get; set; } = null!;

    public virtual VoucherCharge VoucherCharge { get; set; } = null!;

    public virtual VoucherLine VoucherLine { get; set; } = null!;
}
