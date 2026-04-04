using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TransferOrder
{
    public int Id { get; set; }

    public string TransferNumber { get; set; } = null!;

    public int FromBranchId { get; set; }

    public int ToBranchId { get; set; }

    public DateOnly TransferDate { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? ShippedDate { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public int? ShippedBy { get; set; }

    public int? ReceivedBy { get; set; }

    public string? Notes { get; set; }
}
