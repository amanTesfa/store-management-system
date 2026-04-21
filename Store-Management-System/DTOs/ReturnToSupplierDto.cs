namespace Store_Management_System.DTOs
{
    public class ReturnToSupplierDto : BaseDto
    {
        public string VoucherNumber { get; set; } = string.Empty;
        public int OriginalReceiptId { get; set; }
        public string? OriginalReceiptNumber { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateOnly ReturnDate { get; set; }
        public string? ReturnReason { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}