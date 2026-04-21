namespace Store_Management_System.DTOs
{
    public class GoodsReceiptDto : BaseDto
    {
        public string VoucherNumber { get; set; } = string.Empty;
        public int PurchaseOrderId { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateOnly ReceiptDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}