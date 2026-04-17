using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    // ==================== SALES ORDER ====================

    public class SalesOrderIndexViewModel
    {
        public List<SalesOrderListViewModel> SalesOrders { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? Status { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList Statuses { get; set; }
    }

    public class SalesOrderListViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateOnly VoucherDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int ShippedCount { get; set; }
    }

    public class SalesOrderViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Warehouse is required")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Delivery date is required")]
        [DataType(DataType.Date)]
        public DateTime? DeliveryDate { get; set; }

        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";

        // Tax & Charges
        public decimal? TaxRate { get; set; }
        public decimal? ShippingCharge { get; set; }
        public decimal? DiscountAmount { get; set; }

        // Line items
        public List<SalesOrderLineViewModel> Lines { get; set; } = new();

        // Summary
        public decimal SubTotal => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;
        public decimal TotalDiscount => DiscountAmount ?? 0;
        public decimal TaxAmount => (SubTotal - TotalDiscount) * (TaxRate ?? 0) / 100;
        public decimal TotalAmount => SubTotal - TotalDiscount + TaxAmount + (ShippingCharge ?? 0);

        // Approval info
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalComments { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Dropdowns
        public SelectList Customers { get; set; }
        public SelectList Warehouses { get; set; }
        public SelectList Products { get; set; }
    }

    public class SalesOrderLineViewModel
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
        public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);

        public decimal QuantityShipped { get; set; }
        public string? Description { get; set; }

        // Stock info
        public decimal AvailableStock { get; set; }
        public bool IsStockSufficient => AvailableStock >= Quantity;
    }

    // ==================== INVOICE ====================

    public class InvoiceViewModel2
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Posted, Paid
        public string? Remarks { get; set; }

        public List<InvoiceLineViewModel2> Lines { get; set; } = new();

        public SelectList SalesOrders { get; set; }
    }

    public class InvoiceLineViewModel2
    {
        public int Id { get; set; }
        public int SalesOrderLineId { get; set; }
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    // ==================== CUSTOMER RETURN ====================

    public class CustomerReturnViewModel
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;

        [Required]
        public int OriginalInvoiceId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        public string? ReturnReason { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";

        public List<CustomerReturnLineViewModel> Lines { get; set; } = new();

        public SelectList Invoices { get; set; }
        public SelectList Warehouses { get; set; }
    }

    public class CustomerReturnLineViewModel
    {
        public int Id { get; set; }
        public int InvoiceLineId { get; set; }
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public decimal InvoicedQuantity { get; set; }
        public decimal PreviouslyReturned { get; set; }
        public decimal AvailableToReturn { get; set; }
        public decimal QuantityToReturn { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => QuantityToReturn * UnitPrice;
        public string? Reason { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
    }
}