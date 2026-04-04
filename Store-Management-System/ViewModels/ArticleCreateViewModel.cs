using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class ArticleCreateViewModel
{
    // Auto-generated, read-only
    [Display(Name = "Article Code")]
    public string ArticleCode { get; set; } = string.Empty;

    // Auto-generated from ArticleCode, but user can modify
    [Display(Name = "Primary Barcode")]
    public string PrimaryBarcode { get; set; } = string.Empty;

    // Can add additional barcodes
    [Display(Name = "Additional Barcodes")]
    public List<string> AdditionalBarcodes { get; set; } = new List<string>();

    // Article fields (matching your table)
    [Required]
    [Display(Name = "Article Name")]
    public string ArticleName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int? ArticleCategory { get; set; }  // This is CategoryId

    [Display(Name = "Article Group")]
    public string? ArticleGroup { get; set; }

    // Units
    [Required]
    [Display(Name = "Base Unit")]
    public int BaseUnitId { get; set; }

    [Display(Name = "Purchase Unit")]
    public int? PurchaseUnitId { get; set; }

    [Display(Name = "Sales Unit")]
    public int? SalesUnitId { get; set; }

    // Pricing
    [Required]
    [Display(Name = "Standard Cost")]
    public decimal StandardCost { get; set; }

    [Required]
    [Display(Name = "Standard Price")]
    public decimal StandardPrice { get; set; }

    // Inventory
    [Display(Name = "Reorder Level")]
    public decimal ReorderLevel { get; set; }

    [Display(Name = "Max Stock Level")]
    public decimal? MaxStockLevel { get; set; }

    [Display(Name = "Safety Stock")]
    public decimal SafetyStock { get; set; }

    // Status Flags
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Stockable")]
    public bool IsStockable { get; set; } = true;

    [Display(Name = "Purchasable")]
    public bool IsPurchasable { get; set; } = true;

    [Display(Name = "Sellable")]
    public bool IsSellable { get; set; } = true;

    // Tracking
    [Display(Name = "Serialized")]
    public bool IsSerialized { get; set; }

    [Display(Name = "Batch Tracked")]
    public bool IsBatchTracked { get; set; }

    [Display(Name = "Expiry Tracked")]
    public bool IsExpiryTracked { get; set; }

    // Dimensions
    [Display(Name = "Weight")]
    public decimal? Weight { get; set; }

    [Display(Name = "Length")]
    public decimal? Length { get; set; }

    [Display(Name = "Width")]
    public decimal? Width { get; set; }

    [Display(Name = "Height")]
    public decimal? Height { get; set; }

    // Tax
    [Display(Name = "Tax Rate (%)")]
    public decimal TaxRate { get; set; }

    [Display(Name = "Tax Group")]
    public string? TaxGroup { get; set; }

    // Dropdown data
    public SelectList Categories { get; set; }
    public SelectList BaseUnits { get; set; }
    public SelectList PurchaseUnits { get; set; }
    public SelectList SalesUnits { get; set; }
}