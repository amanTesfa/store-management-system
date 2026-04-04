using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class SupplierWithholdingMapping
{
    public int Id { get; set; }

    public int ConsignorId { get; set; }

    public int WithholdingRuleId { get; set; }

    public bool HasTaxRegistration { get; set; }

    public string? ExemptionCertificate { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual Consignor Consignor { get; set; } = null!;

    public virtual WithholdingTaxRule WithholdingRule { get; set; } = null!;
}
