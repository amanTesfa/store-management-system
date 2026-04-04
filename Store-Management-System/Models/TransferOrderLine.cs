using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class TransferOrderLine
{
    public int Id { get; set; }

    public int TransferOrderId { get; set; }

    public int ArticleId { get; set; }

    public decimal QuantityRequested { get; set; }

    public decimal? QuantityShipped { get; set; }

    public decimal? QuantityReceived { get; set; }

    public decimal? UnitCost { get; set; }
}
