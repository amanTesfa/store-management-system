using System;
using System.Collections.Generic;

namespace Store_Management_System.Models
{
    public class StockAdjustment
    {
        public int Id { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public DateOnly AdjustmentDate { get; set; }
        public string AdjustmentType { get; set; } = string.Empty;
        public string ReasonCategory { get; set; } = string.Empty;
        public string? ReasonDescription { get; set; }
        public string? ReferenceNumber { get; set; }
        public string Status { get; set; } = "Draft";
        public decimal TotalValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? SubmittedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedComments { get; set; }
        public DateTime? PostedAt { get; set; }
        public int? PostedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User? SubmittedByNavigation { get; set; }
        public virtual User? ApprovedByNavigation { get; set; }
        public virtual User? PostedByNavigation { get; set; }
        public virtual User? UpdatedByNavigation { get; set; }
        public virtual ICollection<StockAdjustmentLine> StockAdjustmentLines { get; set; } = new List<StockAdjustmentLine>();
    }
}