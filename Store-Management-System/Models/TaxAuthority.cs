using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TaxAuthority
{
    public int Id { get; set; }

    public string AuthorityCode { get; set; } = null!;

    public string AuthorityName { get; set; } = null!;

    public string AuthorityType { get; set; } = null!;

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public virtual ICollection<TaxRule> TaxRules { get; set; } = new List<TaxRule>();

    public virtual ICollection<WithholdingTransaction> WithholdingTransactions { get; set; } = new List<WithholdingTransaction>();
}
