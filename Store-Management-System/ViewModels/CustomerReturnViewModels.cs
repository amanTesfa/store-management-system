using Microsoft.AspNetCore.Mvc.Rendering;

namespace Store_Management_System.ViewModels
{
    // Index/List ViewModels
    public class CustomerReturnIndexViewModel
    {
        public List<CustomerReturnListViewModel> Returns { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class CustomerReturnListViewModel
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public int? OriginalVoucherId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateOnly VoucherDate { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Create/Edit ViewModel
    public class CustomerReturnViewModel
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalInvoiceId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int WarehouseId { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? ReturnReason { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Draft";

        public List<CustomerReturnLineViewModel> Lines { get; set; } = new();

        // Dropdown data
        public SelectList? Invoices { get; set; }
        public SelectList? Warehouses { get; set; }
    }

    public class CustomerReturnLineViewModel
    {
        public int Id { get; set; }
        public int InvoiceLineId { get; set; }
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public decimal SoldQuantity { get; set; }
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