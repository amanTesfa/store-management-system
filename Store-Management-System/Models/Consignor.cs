using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Consignor
{
   // public string Name { get; set; } = null!;
    public int Id { get; set; }

    public string ConsignorCode { get; set; } = null!;

    public string ConsignorName { get; set; } = null!;

    public string ConsignorType { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Mobile { get; set; }

    public string? Fax { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? PaymentTerms { get; set; }

    public decimal? CreditLimit { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public int? Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<SupplierWithholdingMapping> SupplierWithholdingMappings { get; set; } = new List<SupplierWithholdingMapping>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
