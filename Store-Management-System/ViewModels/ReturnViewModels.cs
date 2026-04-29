using Microsoft.AspNetCore.Mvc.Rendering;

namespace Store_Management_System.ViewModels
{
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

    public class ReturnToSupplierViewModel
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalReceiptId { get; set; }
        public string? OriginalReceiptNumber { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int WarehouseId { get; set; }
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
        public decimal ReceivedQuantity { get; set; }
        public decimal PreviouslyReturned { get; set; }
        public decimal AvailableToReturn { get; set; }
        public decimal QuantityToReturn { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue => QuantityToReturn * UnitCost;
        public string? Reason { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}