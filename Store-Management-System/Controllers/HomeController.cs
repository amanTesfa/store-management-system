using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Store_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly InventoryDbContext _context;

        public HomeController(InventoryDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Dashboard statistics
                var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
                var lowStockProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.CurrentStock <= p.ReorderLevel);
                var outOfStock = await _context.Products.CountAsync(p => !p.IsDeleted && p.CurrentStock == 0);
                var totalSuppliers = await _context.Suppliers.CountAsync(s => !s.IsDeleted);
                var totalCategories = await _context.Categories.CountAsync(c => !c.IsDeleted);
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var totalOrders = await _context.Orders.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                // Recent orders
                var recentOrders = await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();

                // Low stock products
                var lowStockItems = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => !p.IsDeleted && p.CurrentStock <= p.ReorderLevel)
                    .Take(5)
                    .ToListAsync();

                // Top selling products (based on order items)
                var topProducts = await _context.OrderItems
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalSold = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .Join(_context.Products, x => x.ProductId, p => p.Id, (x, p) => new { p.Name, x.TotalSold })
                    .ToListAsync();

                // Monthly sales data for chart
                var monthlySales = await _context.Orders
                    .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-6))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new
                    {
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        Total = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(g => g.Year)
                    .ThenBy(g => g.Month)
                    .ToListAsync();

                ViewBag.TotalProducts = totalProducts;
                ViewBag.LowStockProducts = lowStockProducts;
                ViewBag.OutOfStock = outOfStock;
                ViewBag.TotalSuppliers = totalSuppliers;
                ViewBag.TotalCategories = totalCategories;
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.RecentOrders = recentOrders;
                ViewBag.LowStockItems = lowStockItems;
                ViewBag.TopProducts = topProducts;
                ViewBag.MonthlySales = monthlySales;

                return View();
            }

            return View();
        }
        [AllowAnonymous]
        public IActionResult Welcome()
        {
            // If user is already logged in, redirect to dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [Authorize]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index");
        }
    }
}