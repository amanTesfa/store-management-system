using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    // For Index/List page
    public class ArticleIndexViewModel
    {
        public List<ArticleListViewModel> Articles { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public SelectList Categories { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    // For each row in the list
    public class ArticleListViewModel
    {
        public int Id { get; set; }
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public decimal StandardPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public bool IsActive { get; set; }
    }

    // For Create page
    public class ArticleCreateViewModel
    {
        [Display(Name = "Article Code")]
        public string ArticleCode { get; set; } = string.Empty;

        [Display(Name = "Primary Barcode")]
        public string PrimaryBarcode { get; set; } = string.Empty;

        [Display(Name = "Article Name")]
        [Required(ErrorMessage = "Article name is required")]
        public string ArticleName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Article Group")]
        public string? ArticleGroup { get; set; }

        [Display(Name = "Base Unit")]
        [Required(ErrorMessage = "Base unit is required")]
        public int BaseUnitId { get; set; }

        [Display(Name = "Purchase Unit")]
        public int? PurchaseUnitId { get; set; }

        [Display(Name = "Sales Unit")]
        public int? SalesUnitId { get; set; }

        [Display(Name = "Standard Cost")]
        [Required(ErrorMessage = "Standard cost is required")]
        public decimal StandardCost { get; set; }

        [Display(Name = "Standard Price")]
        [Required(ErrorMessage = "Standard price is required")]
        public decimal StandardPrice { get; set; }

        [Display(Name = "Reorder Level")]
        public decimal ReorderLevel { get; set; }

        [Display(Name = "Max Stock Level")]
        public decimal? MaxStockLevel { get; set; }

        [Display(Name = "Safety Stock")]
        public decimal SafetyStock { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Stockable")]
        public bool IsStockable { get; set; } = true;

        [Display(Name = "Purchasable")]
        public bool IsPurchasable { get; set; } = true;

        [Display(Name = "Sellable")]
        public bool IsSellable { get; set; } = true;

        [Display(Name = "Serialized")]
        public bool IsSerialized { get; set; }

        [Display(Name = "Batch Tracked")]
        public bool IsBatchTracked { get; set; }

        [Display(Name = "Expiry Tracked")]
        public bool IsExpiryTracked { get; set; }

        [Display(Name = "Weight")]
        public decimal? Weight { get; set; }

        [Display(Name = "Length")]
        public decimal? Length { get; set; }

        [Display(Name = "Width")]
        public decimal? Width { get; set; }

        [Display(Name = "Height")]
        public decimal? Height { get; set; }

        [Display(Name = "Tax Rate (%)")]
        public decimal TaxRate { get; set; }

        [Display(Name = "Tax Group")]
        public string? TaxGroup { get; set; }

        // For additional barcodes
        public List<string> AdditionalBarcodes { get; set; } = new List<string>();

        // Dropdown data
        public SelectList Categories { get; set; }
        public SelectList BaseUnits { get; set; }
        public SelectList PurchaseUnits { get; set; }
        public SelectList SalesUnits { get; set; }
    }

    // For Edit page
    public class ArticleEditViewModel : ArticleCreateViewModel
    {
        public int Id { get; set; }
    }
}