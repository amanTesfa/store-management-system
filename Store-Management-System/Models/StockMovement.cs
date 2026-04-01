using System;

namespace Store_Management_System.Models
{
    public partial class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string MovementType { get; set; } = null!;
        public string? Reference { get; set; }
        public int PerformedBy { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual User PerformedByNavigation { get; set; } = null!;
    }
}