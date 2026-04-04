using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class VoucherLine
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int LineNumber { get; set; }

    public int ArticleId { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal? ShippedQuantity { get; set; }

    public decimal? ReceivedQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int UnitId { get; set; }

    public decimal ConversionFactor { get; set; }

    public string? SerialNumber { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public int WarehouseId { get; set; }

    public int? StorageLocationId { get; set; }

    public string? Reference { get; set; }

    public bool IsFulfilled { get; set; }

    public decimal FulfilledQuantity { get; set; }

    public string? TaxDetailsJson { get; set; }

    public string? AllocatedChargesJson { get; set; }

    public decimal WithholdingAmount { get; set; }

    public string? WithholdingTaxCode { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual ICollection<ChargeAllocation> ChargeAllocations { get; set; } = new List<ChargeAllocation>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual StorageLocation? StorageLocation { get; set; }

    public virtual UnitOfMeasure Unit { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
