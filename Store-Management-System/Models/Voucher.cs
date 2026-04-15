using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Voucher
{
    public int Id { get; set; }

    public string VoucherNumber { get; set; } = null!;

    public string VoucherType { get; set; } = null!;

    public int ActivityId { get; set; }

    public int? ConsignorId { get; set; }

    public int? ConsigneeId { get; set; }

    public DateOnly VoucherDate { get; set; }

    public DateOnly PostingDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public decimal ExchangeRate { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateOnly? ReferenceDate { get; set; }

    public string Status { get; set; } = null!;

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Remarks { get; set; }

    public string? TermsAndConditions { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
    public int? WarehouseId { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateTime? SentToSupplierAt { get; set; }
    public DateTime? PartiallyReceivedAt { get; set; }
    public DateTime? FullyReceivedAt { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? HandlingCost { get; set; }
    public decimal? InsuranceCost { get; set; }
    public decimal? OtherCost { get; set; }
    public decimal? TotalLandedCost { get; set; }
    public string? LandedCostDistributionMethod { get; set; }
    public bool IsReturn { get; set; }
    public int? OriginalVoucherId { get; set; }
    public string? ReturnReason { get; set; }
    //public DateTime? ApprovedAt { get; set; }
   // public int? ApprovedBy { get; set; }
    public string? ApprovalComments { get; set; }
    public virtual Voucher? OriginalVoucher { get; set; }
    public virtual ICollection<Voucher> ReturnVouchers { get; set; } = new List<Voucher>();
    public virtual User? ApprovedByNavigation { get; set; }
    public virtual ICollection<AccountLedger> AccountLedgers { get; set; } = new List<AccountLedger>();
    // Add these navigation properties
    public virtual Warehouse? Warehouse { get; set; }
    public virtual Activity Activity { get; set; } = null!;

    public virtual ICollection<ClosedRelation> ClosedRelationClosingVouchers { get; set; } = new List<ClosedRelation>();

    public virtual ICollection<ClosedRelation> ClosedRelationOriginalVouchers { get; set; } = new List<ClosedRelation>();

    public virtual Consignee? Consignee { get; set; }

    public virtual Consignor? Consignor { get; set; }

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<TransactionReference> TransactionReferenceSourceVouchers { get; set; } = new List<TransactionReference>();

    public virtual ICollection<TransactionReference> TransactionReferenceTargetVouchers { get; set; } = new List<TransactionReference>();

    public virtual ICollection<VoucherCharge> VoucherCharges { get; set; } = new List<VoucherCharge>();

    public virtual ICollection<VoucherLine> VoucherLines { get; set; } = new List<VoucherLine>();

    public virtual ICollection<WithholdingTransaction> WithholdingTransactions { get; set; } = new List<WithholdingTransaction>();
}
