using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TaxRule
{
    public int Id { get; set; }

    public string TaxCode { get; set; } = null!;

    public string TaxName { get; set; } = null!;

    public int TaxAuthorityId { get; set; }

    public decimal TaxRate { get; set; }

    public string TaxType { get; set; } = null!;

    public string CalculationMethod { get; set; } = null!;

    public bool IsCompound { get; set; }

    public bool IsActive { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public int Priority { get; set; }

    public virtual ICollection<CustomerTaxExemption> CustomerTaxExemptions { get; set; } = new List<CustomerTaxExemption>();

    public virtual TaxAuthority TaxAuthority { get; set; } = null!;

    public virtual ICollection<TaxGroupRule> TaxGroupRules { get; set; } = new List<TaxGroupRule>();
}
