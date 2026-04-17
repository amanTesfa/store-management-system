using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class GoodsReceiptIndexViewModel
    {
        public List<GoodsReceiptListViewModel> GoodsReceipts { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }
    public class GoodsReceiptListViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Voucher number is required")]
        public string VoucherNumber { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
    public class GoodsReceiptViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purchase Order is required")]
        public int PurchaseOrderId { get; set; }

        public string? PONumber { get; set; }
        public string? SupplierName { get; set; }

        [Required(ErrorMessage = "Receipt date is required")]
        [DataType(DataType.Date)]
        public DateTime ReceiptDate { get; set; }

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";
        public List<GoodsReceiptLineViewModel> Lines { get; set; } = new List<GoodsReceiptLineViewModel>();

        public SelectList PurchaseOrders { get; set; }
    }
    public class GoodsReceiptLineViewModel
    {
        public int Id { get; set; }

        public int PurchaseOrderLineId { get; set; }

        public int ArticleId { get; set; }

        public string? ArticleCode { get; set; }

        public string? ArticleName { get; set; }

        [Display(Name = "Ordered Quantity")]
        public decimal OrderedQuantity { get; set; }

        [Display(Name = "Previously Received")]
        public decimal PreviouslyReceived { get; set; }

        [Display(Name = "Available to Receive")]
        public decimal AvailableToReceive { get; set; }

        [Display(Name = "Quantity to Receive")]
        [Range(0, 999999.99, ErrorMessage = "Quantity must be a positive number")]
        public decimal QuantityToReceive { get; set; }

        [Display(Name = "Unit Price")]
        [Range(0, 999999.99, ErrorMessage = "Unit price must be a positive number")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Landed Cost Amount")]
        public decimal LandedCostAmount { get; set; }

        [Display(Name = "Final Unit Cost")]
        public decimal FinalUnitCost { get; set; }

        [Display(Name = "Accept Item")]
        public bool IsAccepted { get; set; } = true;

        [Display(Name = "Rejection Reason")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Batch/Lot Number")]
        [StringLength(100, ErrorMessage = "Batch number cannot exceed 100 characters")]
        public string? BatchNumber { get; set; }

        [Display(Name = "Serial Number")]
        [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        // Product tracking flags (for UI visibility)
        public bool IsBatchTracked { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsExpiryTracked { get; set; }

        // Calculated property
        public decimal LineTotal => QuantityToReceive * FinalUnitCost;
    }
}