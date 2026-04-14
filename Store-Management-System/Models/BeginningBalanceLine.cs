using System;

namespace Store_Management_System.Models
{
    public partial class BeginningBalanceLine
    {
        public int Id { get; set; }
        public int BeginningBalanceId { get; set; }
        public int ArticleId { get; set; }
        public int? WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        //public decimal TotalValue { get; set; }
        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // Navigation properties
        public virtual BeginningBalance BeginningBalance { get; set; } = null!;
        public virtual Article Article { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; }
        public virtual User? CreatedByNavigation { get; set; }
    }
}