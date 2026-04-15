using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    // ==================== PURCHASE ORDER ====================

    public class PurchaseOrderIndexViewModel
    {
        public List<PurchaseOrderListViewModel> PurchaseOrders { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? Status { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList Statuses { get; set; }
    }

    public class PurchaseOrderListViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateOnly VoucherDate { get; set; }
        public DateOnly? ExpectedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int ReceivedCount { get; set; }
    }

    public class PurchaseOrderViewModel
    {
        public int Id { get; set; }
        public DateOnly VoucherDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public string VoucherNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Supplier is required")]
        public int ConsignorId { get; set; }

        [Required(ErrorMessage = "Warehouse is required")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Expected date is required")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";

        // Charges
        public decimal? ShippingCost { get; set; }
        public decimal? HandlingCost { get; set; }
        public decimal? InsuranceCost { get; set; }
        public decimal? OtherCost { get; set; }
        public string? LandedCostDistributionMethod { get; set; } = "ByValue";

        // Approval
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalComments { get; set; }

        // Line items
        public List<PurchaseOrderLineViewModel> Lines { get; set; } = new();

        // Summary
        public decimal SubTotal => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;
        public decimal TotalCharges => (ShippingCost ?? 0) + (HandlingCost ?? 0) + (InsuranceCost ?? 0) + (OtherCost ?? 0);
        public decimal TotalAmount => SubTotal + TotalCharges;

        // Dropdowns
        public SelectList Suppliers { get; set; }
        public SelectList Warehouses { get; set; }
        public SelectList Products { get; set; }
        public SelectList DistributionMethods { get; set; }
    }

    public class PurchaseOrderLineViewModel
    {
        public int Id { get; set; }
        public int? VoucherId { get; set; }

        [Required(ErrorMessage = "Product is required")]
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, 999999.99)]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 999999.99)]
        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount => Quantity * UnitPrice * (DiscountPercent / 100);
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

        public decimal? LandedCostPercentage { get; set; }
        public decimal? LandedCostAmount { get; set; }
        public decimal? FinalUnitCost { get; set; }

        public decimal QuantityReceived { get; set; }
        public decimal QuantityReturned { get; set; }

        public string? Description { get; set; }
    }

    // ==================== GOODS RECEIPT NOTE ====================

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

        public List<GoodsReceiptLineViewModel> Lines { get; set; } = new();

        public SelectList PurchaseOrders { get; set; }
    }

    public class GoodsReceiptLineViewModel
    {
        public int Id { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal PreviouslyReceived { get; set; }
        public decimal QuantityToReceive { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LandedCostAmount { get; set; }
        public decimal FinalUnitCost { get; set; }
        public bool IsAccepted { get; set; } = true;
        public string? RejectionReason { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }

    // ==================== RETURN TO SUPPLIER ====================

    public class ReturnToSupplierViewModel
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;

        [Required]
        public int OriginalReceiptId { get; set; }

        public string? OriginalReceiptNumber { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public string? SupplierName { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        public string? ReturnReason { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";

        public List<ReturnToSupplierLineViewModel> Lines { get; set; } = new();

        public SelectList Receipts { get; set; }
        public SelectList Warehouses { get; set; }
    }

    public class ReturnToSupplierLineViewModel
    {
        public int Id { get; set; }
        public int ReceiptLineId { get; set; }
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal QuantityToReturn { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue => QuantityToReturn * UnitCost;
        public string? Reason { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
    }

    // ==================== RETURN INDEX ====================

    public class ReturnIndexViewModel
    {
        public List<ReturnListViewModel> Returns { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class ReturnListViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public int? OriginalVoucherId { get; set; }
        public string? OriginalVoucherNumber { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateOnly VoucherDate { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}