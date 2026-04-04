using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ChargeType
{
    public int Id { get; set; }

    public string ChargeCode { get; set; } = null!;

    public string ChargeName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string CalculationMethod { get; set; } = null!;

    public decimal? DefaultRate { get; set; }

    public string? GlaccountCode { get; set; }

    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<VoucherCharge> VoucherCharges { get; set; } = new List<VoucherCharge>();
}
