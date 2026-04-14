using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    // For Index/List page
    public class BeginningBalanceIndexViewModel
    {
        public List<BeginningBalanceListViewModel> Balances { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? Status { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList Statuses { get; set; }
    }

    public class BeginningBalanceListViewModel
    {
        public int Id { get; set; }
        public string BalanceNumber { get; set; } = string.Empty;
        public string PeriodName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PostedAt { get; set; }
    }

    // For Create/Edit - Main ViewModel
    public class BeginningBalanceViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Balance Number")]
        public string BalanceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fiscal period is required")]
        [Display(Name = "Fiscal Period")]
        public int FiscalPeriodId { get; set; }

        [Display(Name = "Warehouse")]
        public int? WarehouseId { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        public string Status { get; set; } = "Draft";

        public List<BeginningBalanceLineViewModel> Lines { get; set; } = new List<BeginningBalanceLineViewModel>();

        // Summary
        public decimal TotalValue => Lines?.Sum(l => l.TotalValue) ?? 0;
        public int TotalItems => Lines?.Count ?? 0;

        // Dropdowns
        public SelectList FiscalPeriods { get; set; }
        public SelectList Warehouses { get; set; }
        public SelectList Articles { get; set; }

        // Approval info
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? PostedAt { get; set; }
        public int? PostedBy { get; set; }
        public string? PostedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
    }
    public class BeginningBalanceDetailsViewModel
    {
        public int Id { get; set; }
        public string BalanceNumber { get; set; } = string.Empty;
        public string? FiscalPeriodName { get; set; }
        public string? WarehouseName { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? PostedByName { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public List<BeginningBalanceLineDetailsViewModel> Lines { get; set; } = new();
    }

    public class BeginningBalanceLineDetailsViewModel
    {
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string? WarehouseName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }
    // Line item ViewModel
    public class BeginningBalanceLineViewModel
    {
        public int Id { get; set; }
        public int? BeginningBalanceId { get; set; }

        [Required(ErrorMessage = "Product is required")]
        [Display(Name = "Product")]
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }

        [Display(Name = "Warehouse")]
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unit cost is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Unit cost must be greater than 0")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        public decimal TotalValue => Quantity * UnitCost;

        // Batch/Serial fields (dynamic)
        [Display(Name = "Batch/Lot Number")]
        public string? BatchNumber { get; set; }

        [Display(Name = "Serial Number")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Product tracking flags (for UI)
        public bool IsBatchTracked { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsExpiryTracked { get; set; }
    }

    // For Approval
    public class ApprovalViewModel
    {
        public int Id { get; set; }
        public string? Comments { get; set; }
    }
}