namespace Store_Management_System.DTOs
{
    public class BeginningBalanceImportDto
    {
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class BeginningBalanceImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public int BalanceId { get; set; }
        public string BalanceNumber { get; set; } = string.Empty;
    }
}