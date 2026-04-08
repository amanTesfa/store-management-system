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

    public string? Tin { get; set; }

    public string? Industry { get; set; }

    public int? NumberOfEmployees { get; set; }

    public decimal? AnnualRevenue { get; set; }

    public string? Website { get; set; }

    public int? ParentCompanyId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? NationalId { get; set; }

    public string? Occupation { get; set; }

    public string? Reserved1 { get; set; }

    public string? Reserved2 { get; set; }

    public string? Reserved3 { get; set; }

    public string? Reserved4 { get; set; }

    public string? Reserved5 { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<CustomerTaxExemption> CustomerTaxExemptions { get; set; } = new List<CustomerTaxExemption>();

    public virtual ICollection<Consignee> InverseParentCompany { get; set; } = new List<Consignee>();

    public virtual Consignee? ParentCompany { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
