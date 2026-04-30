using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class CustomerTaxExemption
{
    public int Id { get; set; }

    public int ConsigneeId { get; set; }

    public int TaxRuleId { get; set; }

    public string? ExemptionCertificate { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    public string? Reason { get; set; }

    public virtual Consignee Consignee { get; set; } = null!;

    public virtual TaxRule TaxRule { get; set; } = null!;
}
