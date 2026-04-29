namespace Store_Management_System.DTOs
{
    public class StockMovementDto
    {
        public int Id { get; set; }
        public string MovementNumber { get; set; } = string.Empty;
        public int ArticleId { get; set; }
        public string? ArticleCode { get; set; }
        public string? ArticleName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime MovementDate { get; set; }
    }
}