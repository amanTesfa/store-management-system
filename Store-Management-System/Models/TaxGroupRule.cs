using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TaxGroupRule
{
    public int Id { get; set; }

    public int TaxGroupId { get; set; }

    public int TaxRuleId { get; set; }

    public bool IsDefault { get; set; }

    public virtual TaxGroup TaxGroup { get; set; } = null!;

    public virtual TaxRule TaxRule { get; set; } = null!;
}
