using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class UnitOfMeasure
{
    public int Id { get; set; }

    public string UnitCode { get; set; } = null!;

    public string UnitName { get; set; } = null!;

    public string UnitType { get; set; } = null!;

    public decimal ConversionFactor { get; set; }

    public int? BaseUnitId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Article> ArticleBaseUnits { get; set; } = new List<Article>();

    public virtual ICollection<Article> ArticlePurchaseUnits { get; set; } = new List<Article>();

    public virtual ICollection<Article> ArticleSalesUnits { get; set; } = new List<Article>();

    public virtual UnitOfMeasure? BaseUnit { get; set; }

    public virtual ICollection<UnitOfMeasure> InverseBaseUnit { get; set; } = new List<UnitOfMeasure>();

    public virtual ICollection<VoucherLine> VoucherLines { get; set; } = new List<VoucherLine>();
}
