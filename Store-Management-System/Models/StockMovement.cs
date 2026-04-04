using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class StockMovement
{
    public int Id { get; set; }

    public string MovementNumber { get; set; } = null!;

    public int VoucherId { get; set; }

    public int VoucherLineId { get; set; }

    public int ArticleId { get; set; }

    public int WarehouseId { get; set; }

    public int? StorageLocationId { get; set; }

    public string MovementType { get; set; } = null!;

    public string ActivityType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal PreviousStock { get; set; }

    public decimal NewStock { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }

    public string? SerialNumber { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateTime MovementDate { get; set; }

    public int CreatedBy { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;

    public virtual VoucherLine VoucherLine { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
