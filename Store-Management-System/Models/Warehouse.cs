using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Warehouse
{
    public int Id { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public string? Location { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<BeginningBalanceLine> BeginningBalanceLines { get; set; } = new List<BeginningBalanceLine>();

    public virtual ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();

    public virtual ICollection<VoucherLine> VoucherLines { get; set; } = new List<VoucherLine>();
}
