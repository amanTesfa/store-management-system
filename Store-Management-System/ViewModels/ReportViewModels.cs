namespace Store_Management_System.ViewModels
{
    public class CurrentStockReportViewModel
    {
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class LowStockReportViewModel
    {
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int Shortage { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PotentialLoss { get; set; }
    }

    public class StockMovementReportViewModel
    {
        public DateTime MovementDate { get; set; }
        public string MovementNumber { get; set; } = string.Empty;
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    public class StockValuationCategoryViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }
}