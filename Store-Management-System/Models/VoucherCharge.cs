using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class VoucherCharge
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int ChargeTypeId { get; set; }

    public decimal ChargeAmount { get; set; }

    public decimal? ChargeRate { get; set; }

    public decimal? CalculationBasis { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Description { get; set; }

    public bool IsAllocated { get; set; }

    public virtual ICollection<ChargeAllocation> ChargeAllocations { get; set; } = new List<ChargeAllocation>();

    public virtual ChargeType ChargeType { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
