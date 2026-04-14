using System;
using System.Collections.Generic;

namespace Store_Management_System.Models
{
    public partial class BeginningBalance
    {
        public int Id { get; set; }
        public string BalanceNumber { get; set; } = null!;
        public DateOnly BalanceDate { get; set; }
        public int FiscalPeriodId { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!; // Draft, Approved, Posted
        public DateTime? PostedAt { get; set; }
        public int? PostedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovalComments { get; set; }
        public int? WarehouseId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual FiscalPeriod FiscalPeriod { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; }
        public virtual User? CreatedByNavigation { get; set; }
        public virtual User? ApprovedByNavigation { get; set; }
        public virtual User? PostedByNavigation { get; set; }
        public virtual User? UpdatedByNavigation { get; set; }
        public virtual ICollection<BeginningBalanceLine> BeginningBalanceLines { get; set; } = new List<BeginningBalanceLine>();
    }
}