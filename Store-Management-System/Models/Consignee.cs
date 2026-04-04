using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Consignee
{
    public int Id { get; set; }

    public string ConsigneeCode { get; set; } = null!;

    public string ConsigneeName { get; set; } = null!;

    public string ConsigneeType { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Mobile { get; set; }

    public string? ShippingAddress { get; set; }

    public string? BillingAddress { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? PaymentTerms { get; set; }

    public decimal? CreditLimit { get; set; }

    public decimal CurrentBalance { get; set; }

    public string? PriceLevel { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? CustomerSince { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CustomerTaxExemption> CustomerTaxExemptions { get; set; } = new List<CustomerTaxExemption>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
