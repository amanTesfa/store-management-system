using System;
using System.Collections.Generic;

namespace Store_Management_System.ViewModels
{
    public class OrderDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int LowStock { get; set; }
        public int TotalOrders { get; set; }
        public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
    }
}
