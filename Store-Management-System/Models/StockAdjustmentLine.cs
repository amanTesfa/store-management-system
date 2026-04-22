using System;

namespace Store_Management_System.Models
{
    public class StockAdjustmentLine
    {
        public int Id { get; set; }
        public int AdjustmentId { get; set; }
        public int ArticleId { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal Variance { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public virtual StockAdjustment StockAdjustment { get; set; } = null!;
        public virtual Article Article { get; set; } = null!;
    }
}