namespace Store_Management_System.DTOs
{
    public class BeginningBalanceDto : BaseDto
    {
        public string BalanceNumber { get; set; } = string.Empty;
        public int FiscalPeriodId { get; set; }
        public string? PeriodName { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }
}