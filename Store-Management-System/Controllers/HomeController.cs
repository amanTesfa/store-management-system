using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [AllowAnonymous]
        public IActionResult Welcome()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Dashboard Statistics
            var totalProducts = await _context.Articles.CountAsync(a => a.IsActive);
            var activeProducts = await _context.Articles.CountAsync(a => a.IsActive && a.IsSellable);
            var activePercentage = totalProducts > 0 ? (activeProducts * 100 / totalProducts) : 0;

            // Stock Levels
            var stockLevels = await _context.CurrentStocks
                .GroupBy(cs => cs.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalStock = g.Sum(cs => cs.Quantity) })
                .ToListAsync();

            var articleStockDict = stockLevels.ToDictionary(x => x.ArticleId, x => x.TotalStock);

            var articles = await _context.Articles.Where(a => a.IsActive).ToListAsync();

            var lowStockCount = articles.Count(a =>
                articleStockDict.ContainsKey(a.Id) && articleStockDict[a.Id] <= a.ReorderLevel && articleStockDict[a.Id] > 0);
            var outOfStockCount = articles.Count(a =>
                !articleStockDict.ContainsKey(a.Id) || articleStockDict[a.Id] == 0);

            // Inventory Value
            var totalInventoryValue = await _context.CurrentStocks
                .SumAsync(cs => cs.Quantity * (cs.Article != null ? cs.Article.StandardCost : 0));
            var totalStockQuantity = await _context.CurrentStocks.SumAsync(cs => cs.Quantity);

            // Monthly Sales (from StockMovements with type "Out")
            var currentMonth = DateTime.UtcNow;
            var monthlySalesMovements = await _context.StockMovements
                .Where(sm => sm.MovementDate.Year == currentMonth.Year && sm.MovementDate.Month == currentMonth.Month && sm.MovementType == "Out")
                .ToListAsync();
            var monthlySalesCount = monthlySalesMovements.Sum(sm => (int)sm.Quantity);
            var monthlySalesValue = monthlySalesMovements.Sum(sm => sm.TotalCost);

            // Stock Movement for Chart (Last 6 Months)
            var last6Months = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-i)).OrderBy(d => d).ToList();
            var movementMonths = last6Months.Select(d => d.ToString("MMM yyyy")).ToList();

            var movementIn = new List<decimal>();
            var movementOut = new List<decimal>();

            foreach (var month in last6Months)
            {
                var inQty = await _context.StockMovements
                    .Where(sm => sm.MovementDate.Year == month.Year && sm.MovementDate.Month == month.Month && sm.MovementType == "In")
                    .SumAsync(sm => sm.Quantity);
                var outQty = await _context.StockMovements
                    .Where(sm => sm.MovementDate.Year == month.Year && sm.MovementDate.Month == month.Month && sm.MovementType == "Out")
                    .SumAsync(sm => sm.Quantity);
                movementIn.Add(inQty);
                movementOut.Add(outQty);
            }

            // Stock by Category
            var categoryStock = await _context.CurrentStocks
                .Include(cs => cs.Article)
                    .ThenInclude(a => a.ArticleCategoryNavigation)
                .Where(cs => cs.Quantity > 0 && cs.Article.ArticleCategoryNavigation != null)
                .GroupBy(cs => cs.Article.ArticleCategoryNavigation.Name)
                .Select(g => new { Category = g.Key, Quantity = g.Sum(cs => cs.Quantity) })
                .OrderByDescending(g => g.Quantity)
                .Take(8)
                .ToListAsync();

            var categoryNames = categoryStock.Select(c => c.Category).ToList();
            var categoryValues = categoryStock.Select(c => c.Quantity).ToList();

            // Top Selling Products (from VoucherLines via Sales Orders)
            var topProducts = await _context.VoucherLines
                .Include(vl => vl.Article)
                .Where(vl => vl.Voucher.VoucherType == "SO" && vl.Voucher.Status == "Completed")
                .GroupBy(vl => vl.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalSold = g.Sum(vl => vl.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .Join(_context.Articles, x => x.ArticleId, a => a.Id, (x, a) => new { a.ArticleName, x.TotalSold })
                .ToListAsync();

            var topProductNames = topProducts.Select(p => p.ArticleName.Length > 20 ? p.ArticleName.Substring(0, 20) + "..." : p.ArticleName).ToList();
            var topProductQuantities = topProducts.Select(p => p.TotalSold).ToList();

            // Recent Sales Orders
            var recentOrders = await _context.Vouchers
                .Include(v => v.Consignee)
                .Where(v => v.VoucherType == "SO" && v.Status != "Draft")
                .OrderByDescending(v => v.CreatedAt)
                .Take(10)
                .Select(v => new
                {
                    v.VoucherNumber,
                    CustomerName = v.Consignee != null ? v.Consignee.ConsigneeName : "Unknown",
                    OrderDate = v.VoucherDate,
                    v.TotalAmount,
                    v.Status
                })
                .ToListAsync();

            // Pass data to View
            ViewBag.TotalProducts = totalProducts;
            ViewBag.ActiveProducts = activePercentage;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.OutOfStockCount = outOfStockCount;
            ViewBag.TotalInventoryValue = totalInventoryValue;
            ViewBag.TotalStockQuantity = totalStockQuantity;
            ViewBag.MonthlySalesCount = monthlySalesCount;
            ViewBag.MonthlySalesValue = monthlySalesValue;
            ViewBag.MovementMonths = movementMonths;
            ViewBag.MovementIn = movementIn;
            ViewBag.MovementOut = movementOut;
            ViewBag.CategoryNames = categoryNames;
            ViewBag.CategoryValues = categoryValues;
            ViewBag.TopProductNames = topProductNames;
            ViewBag.TopProductQuantities = topProductQuantities;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }
    }
}