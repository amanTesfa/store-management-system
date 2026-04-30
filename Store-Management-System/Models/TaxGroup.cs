using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TaxGroup
{
    public int Id { get; set; }

    public string GroupCode { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ArticleTaxMapping> ArticleTaxMappings { get; set; } = new List<ArticleTaxMapping>();

    public virtual ICollection<TaxGroupRule> TaxGroupRules { get; set; } = new List<TaxGroupRule>();
}
