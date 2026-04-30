using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class StorageLocation
{
    public int Id { get; set; }

    public int WarehouseId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public string? Zone { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<VoucherLine> VoucherLines { get; set; } = new List<VoucherLine>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
