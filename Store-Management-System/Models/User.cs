using Microsoft.AspNetCore.Identity;
using StoreManagementSystem.Models;
using System;
using System.Collections.Generic;

namespace Store_Management_System.Models
{
    public class User : IdentityUser<int>
    {
        // Custom properties
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}