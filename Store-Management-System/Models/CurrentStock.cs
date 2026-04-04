using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class CurrentStock
{
    public int Id { get; set; }

    public int ArticleId { get; set; }

    public int WarehouseId { get; set; }

    public int? StorageLocationId { get; set; }

    public string? BatchNumber { get; set; }

    public string? SerialNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal Quantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
