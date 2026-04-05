using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Article
{
    public int Id { get; set; }

    public string ArticleCode { get; set; } = null!;

    public string ArticleName { get; set; } = null!;

    public string ArticleType { get; set; } = null!;

    public int? ArticleCategory { get; set; }

    public string? ArticleGroup { get; set; }

    public string? Description { get; set; }

    public int BaseUnitId { get; set; }

    public int? PurchaseUnitId { get; set; }

    public int? SalesUnitId { get; set; }

    public decimal StandardCost { get; set; }

    public decimal StandardPrice { get; set; }

    public decimal? LastPurchasePrice { get; set; }

    public decimal? AverageCost { get; set; }

    public decimal ReorderLevel { get; set; }

    public decimal? MaxStockLevel { get; set; }

    public decimal SafetyStock { get; set; }

    public decimal? ReorderQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsStockable { get; set; }

    public bool IsPurchasable { get; set; }

    public bool IsSellable { get; set; }

    public bool IsSerialized { get; set; }

    public bool IsBatchTracked { get; set; }

    public bool IsExpiryTracked { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public decimal TaxRate { get; set; }

    public string? TaxGroup { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }
    public virtual Category? Category { get; set; }
  
    public virtual ICollection<ArticleBarcode> ArticleBarcodes { get; set; } = new List<ArticleBarcode>();

    public virtual ICollection<ArticlePrice> ArticlePrices { get; set; } = new List<ArticlePrice>();

    public virtual ICollection<ArticleTaxMapping> ArticleTaxMappings { get; set; } = new List<ArticleTaxMapping>();

    public virtual UnitOfMeasure BaseUnit { get; set; } = null!;

    public virtual ICollection<BeginningBalanceLine> BeginningBalanceLines { get; set; } = new List<BeginningBalanceLine>();

    public virtual ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();

    public virtual UnitOfMeasure? PurchaseUnit { get; set; }

    public virtual UnitOfMeasure? SalesUnit { get; set; }

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<VoucherLine> VoucherLines { get; set; } = new List<VoucherLine>();
}
