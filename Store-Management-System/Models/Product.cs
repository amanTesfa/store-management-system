using StoreManagementSystem.Models;
using System;
using System.Collections.Generic;

namespace Store_Management_System.Models
{
    public partial class Product
    {
        public int Id { get; set; }
        public string Sku { get; set; } = null!;  // Note: "Sku" not "SKU"
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int ReorderLevel { get; set; }
        public int CurrentStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}