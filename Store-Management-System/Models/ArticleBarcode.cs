using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ArticleBarcode
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = "CODE128";
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public virtual Article Article { get; set; } = null!;
}
