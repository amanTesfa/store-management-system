using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class ProductIndexViewModel
    {
        public List<ProductListViewModel> Products { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Suppliers { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class ProductListViewModel
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string StockStatus { get; set; } = "In Stock";
        public string StockStatusClass { get; set; } = "success";
        public bool IsActive { get; set; } = true;
    }

    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "SKU must be between 3 and 50 characters")]
        [Display(Name = "SKU")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Supplier")]
        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Unit price must be between 0.01 and 999,999.99")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Cost price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Cost price must be between 0.01 and 999,999.99")]
        [Display(Name = "Cost Price")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, 99999, ErrorMessage = "Reorder level must be between 0 and 99,999")]
        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Display(Name = "Current Stock")]
        public int CurrentStock { get; set; }

        [Display(Name = "Maximum Stock Level")]
        public int? MaxStockLevel { get; set; }

        [Display(Name = "Tax Rate (%)")]
        public decimal TaxRate { get; set; }

        [Display(Name = "Track by Serial Number")]
        public bool IsSerialized { get; set; }

        [Display(Name = "Track by Batch/Lot")]
        public bool IsBatchTracked { get; set; }

        [Display(Name = "Track Expiry Date")]
        public bool IsExpiryTracked { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public SelectList Categories { get; set; }
        public SelectList Suppliers { get; set; }
    }

    public class ProductEditViewModel : ProductCreateViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductDetailsViewModel
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public int CategoryId { get; set; }
        public string? SupplierName { get; set; }
        public int? SupplierId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int ReorderLevel { get; set; }
        public int CurrentStock { get; set; }
        public int? MaxStockLevel { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsBatchTracked { get; set; }
        public bool IsExpiryTracked { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string StockStatusClass { get; set; } = string.Empty;
        public List<RecentStockMovementViewModel> RecentMovements { get; set; } = new();
    }

    public class RecentStockMovementViewModel
    {
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public decimal UnitCost { get; internal set; }
        public string WarehouseName { get; internal set; }
    }
}