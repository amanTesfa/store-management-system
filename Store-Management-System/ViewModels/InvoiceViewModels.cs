using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class InvoiceIndexViewModel
    {
        public List<InvoiceListViewModel> Invoices { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? Status { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList Statuses { get; set; }
    }

    public class InvoiceListViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }

    public class InvoiceViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sales Order is required")]
        public int SalesOrderId { get; set; }

        public string? SalesOrderNumber { get; set; }
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Invoice date is required")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Draft";
        public string? Remarks { get; set; }

        public List<InvoiceLineViewModel> Lines { get; set; } = new();

        public SelectList SalesOrders { get; set; }
    }

    public class InvoiceLineViewModel
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
}