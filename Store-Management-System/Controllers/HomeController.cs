using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store_Management_System.Models;

namespace Store_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly InventoryDbContext _context;

        public HomeController(InventoryDbContext context)
        {
            _context = context;
        }

        // Demo page to preview the Tailwind ERP theme (non-destructive)
        public IActionResult ErpTailwindDemo()
        {
            ViewData["Title"] = "ERP Theme Demo";
            return View("ErpTailwindDemo");
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.Welcome = $"Welcome back, {User.Identity.Name}!";

                // Show dashboard stats for authenticated users
                ViewBag.TotalProducts = _context.Products.Count(p => !p.IsDeleted);
                ViewBag.LowStock = _context.Products.Count(p => !p.IsDeleted && p.CurrentStock <= p.ReorderLevel);
                ViewBag.TotalOrders = _context.Orders.Count();
                ViewBag.RecentOrders = _context.Orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
            }

            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }
    }
}