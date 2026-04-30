namespace Store_Management_System.DTOs
{
    public class InvoiceDto : BaseDto
    {
        public string VoucherNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
    }
}