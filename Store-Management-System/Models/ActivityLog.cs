using System;

namespace Store_Management_System.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Create, Edit, Delete, View, Export, Login, Logout
        public string EntityType { get; set; } = string.Empty; // Article, PurchaseOrder, SalesOrder, etc.
        public int? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}