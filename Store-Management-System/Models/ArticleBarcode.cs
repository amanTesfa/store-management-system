using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ArticleBarcode
{
    public int Id { get; set; }

    public int ArticleId { get; set; }

    public string Barcode { get; set; } = null!;

    public string BarcodeType { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Article Article { get; set; } = null!;
}
