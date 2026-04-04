using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class WithholdingTransaction
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int WithholdingRuleId { get; set; }

    public int TaxAuthorityId { get; set; }

    public decimal BaseAmount { get; set; }

    public decimal WithholdingRate { get; set; }

    public decimal WithholdingAmount { get; set; }

    public DateOnly? RemittanceDate { get; set; }

    public string? RemittanceReference { get; set; }

    public string? CertificateNumber { get; set; }

    public virtual TaxAuthority TaxAuthority { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;

    public virtual WithholdingTaxRule WithholdingRule { get; set; } = null!;
}
