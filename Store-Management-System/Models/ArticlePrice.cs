using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ArticlePrice
{
    public int Id { get; set; }

    public int ArticleId { get; set; }

    public string PriceType { get; set; } = null!;

    public decimal Price { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public bool IsActive { get; set; }

    public virtual Article Article { get; set; } = null!;
}
