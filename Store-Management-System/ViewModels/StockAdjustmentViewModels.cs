using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class StockAdjustmentIndexViewModel
    {
        public List<StockAdjustmentListViewModel> Adjustments { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? Status { get; set; }
        public int? WarehouseId { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList Statuses { get; set; }
        public SelectList Warehouses { get; set; }
    }

    public class StockAdjustmentListViewModel
    {
        public int Id { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly AdjustmentDate { get; set; }
        public string AdjustmentType { get; set; } = string.Empty;
        public string ReasonCategory { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }
    }

    public class StockAdjustmentViewModel
    {
        public int Id { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Warehouse is required")]
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        [Required(ErrorMessage = "Adjustment date is required")]
        [DataType(DataType.Date)]
        public DateTime AdjustmentDate { get; set; }

        [Required(ErrorMessage = "Adjustment type is required")]
        public string AdjustmentType { get; set; } = "WriteOff";

        [Required(ErrorMessage = "Reason category is required")]
        public string ReasonCategory { get; set; } = string.Empty;

        [Display(Name = "Reason Description")]
        [StringLength(500)]
        public string? ReasonDescription { get; set; }

        [Display(Name = "Reference Number")]
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public string Status { get; set; } = "Draft";
        public decimal TotalValue { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<StockAdjustmentLineViewModel> Lines { get; set; } = new();

        // Approval info
        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public string? ApprovedComments { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? PostedByName { get; set; }

        // Dropdowns
        public SelectList Warehouses { get; set; }
        public SelectList Products { get; set; }
        public SelectList AdjustmentTypes { get; set; }
        public SelectList ReasonCategories { get; set; }
    }

    public class StockAdjustmentLineViewModel
    {
        public int Id { get; set; }
        public int? AdjustmentId { get; set; }

        [Required(ErrorMessage = "Product is required")]
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }

        [Display(Name = "System Quantity")]
        public decimal SystemQuantity { get; set; }

        [Display(Name = "Physical Quantity")]
        [Required(ErrorMessage = "Physical quantity is required")]
        public decimal PhysicalQuantity { get; set; }

        public decimal Variance { get; set; }

        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }

        [Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; }

        [Display(Name = "Serial Number")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Product tracking flags
        public bool IsBatchTracked { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsExpiryTracked { get; set; }
    }

    // For Quick Count Mode
    public class QuickCountViewModel
    {
        public int AdjustmentId { get; set; }
        public List<QuickCountItemViewModel> Items { get; set; } = new();
    }

    public class QuickCountItemViewModel
    {
        public int ArticleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public decimal PhysicalQuantity { get; set; }
    }
}