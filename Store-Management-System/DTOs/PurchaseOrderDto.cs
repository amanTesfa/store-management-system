namespace Store_Management_System.DTOs
{
    public class PurchaseOrderDto : BaseDto
    {
        public string VoucherNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateOnly VoucherDate { get; set; }
        public DateOnly? ExpectedDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int LineCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}