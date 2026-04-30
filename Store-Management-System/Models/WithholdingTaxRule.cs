using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class WithholdingTaxRule
{
    public int Id { get; set; }

    public string RuleCode { get; set; } = null!;

    public string RuleName { get; set; } = null!;

    public int TaxAuthorityId { get; set; }

    public decimal TaxRate { get; set; }

    public string CalculationBase { get; set; } = null!;

    public decimal? ThresholdAmount { get; set; }

    public bool IsActive { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual ICollection<SupplierWithholdingMapping> SupplierWithholdingMappings { get; set; } = new List<SupplierWithholdingMapping>();

    public virtual ICollection<WithholdingTransaction> WithholdingTransactions { get; set; } = new List<WithholdingTransaction>();
}
