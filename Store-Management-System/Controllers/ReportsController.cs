using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;
using System.Data;
using System.Text;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly InventoryDbContext _context;

        public ReportsController(InventoryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        // POST: Reports/ExportCurrentStockToExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportCurrentStockToExcel(int? warehouseId = null, int? categoryId = null)
        {
            var query = _context.CurrentStocks
                .Include(cs => cs.Article)
                    .ThenInclude(a => a.ArticleCategoryNavigation)
                .Include(cs => cs.Warehouse)
                .Where(cs => cs.Quantity > 0)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(cs => cs.WarehouseId == warehouseId);
            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(cs => cs.Article.ArticleCategory == categoryId);

            var data = await query
                .Select(cs => new
                {
                    cs.Article.ArticleCode,
                    cs.Article.ArticleName,
                    Category = cs.Article.ArticleCategoryNavigation != null ? cs.Article.ArticleCategoryNavigation.Name : "Uncategorized",
                    Warehouse = cs.Warehouse.WarehouseName,
                    cs.Quantity,
                    UnitCost = cs.Article.StandardCost,
                    TotalValue = cs.Article.StandardCost * cs.Quantity
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("SKU,Product Name,Category,Warehouse,Quantity,Unit Cost,Total Value");
            foreach (var item in data)
            {
                csv.AppendLine($"\"{item.ArticleCode}\",\"{item.ArticleName}\",\"{item.Category}\",\"{item.Warehouse}\",{item.Quantity},{item.UnitCost:F2},{item.TotalValue:F2}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"CurrentStock_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // POST: Reports/ExportLowStockToExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportLowStockToExcel(int? warehouseId = null)
        {
            // Get stock levels from CurrentStock table
            var stockLevels = await _context.CurrentStocks
                .GroupBy(cs => cs.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalStock = g.Sum(cs => cs.Quantity) })
                .ToDictionaryAsync(k => k.ArticleId, v => v.TotalStock);

            var query = _context.Articles
                .Include(a => a.ArticleCategoryNavigation)
                .Where(a => a.IsActive)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
            {
                var stockQuery = _context.CurrentStocks
                    .Where(cs => cs.WarehouseId == warehouseId && cs.Quantity > 0)
                    .Select(cs => cs.ArticleId);
                query = query.Where(a => stockQuery.Contains(a.Id));
            }

            var articles = await query.ToListAsync();

            var data = articles
                .Select(a => new
                {
                    a.ArticleCode,
                    a.ArticleName,
                    Category = a.ArticleCategoryNavigation != null ? a.ArticleCategoryNavigation.Name : "Uncategorized",
                    CurrentStock = stockLevels.ContainsKey(a.Id) ? (int)stockLevels[a.Id] : 0,
                    a.ReorderLevel,
                    Shortage = (int)(a.ReorderLevel - (stockLevels.ContainsKey(a.Id) ? stockLevels[a.Id] : 0)),
                    a.StandardPrice
                })
                .Where(a => a.CurrentStock <= a.ReorderLevel)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("SKU,Product Name,Category,Current Stock,Reorder Level,Shortage,Unit Price");
            foreach (var item in data)
            {
                csv.AppendLine($"\"{item.ArticleCode}\",\"{item.ArticleName}\",\"{item.Category}\",{item.CurrentStock},{item.ReorderLevel},{item.Shortage},{item.StandardPrice:F2}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"LowStock_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // POST: Reports/ExportStockMovementToExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportStockMovementToExcel(DateTime? fromDate = null, DateTime? toDate = null,
            int? articleId = null, string? movementType = null)
        {
            if (!fromDate.HasValue) fromDate = DateTime.UtcNow.AddMonths(-1);
            if (!toDate.HasValue) toDate = DateTime.UtcNow;

            var query = _context.StockMovements
                .Include(s => s.Article)
                .Include(s => s.Warehouse)
                .Where(s => s.MovementDate >= fromDate && s.MovementDate <= toDate)
                .AsQueryable();

            if (articleId.HasValue && articleId > 0)
                query = query.Where(s => s.ArticleId == articleId);
            if (!string.IsNullOrEmpty(movementType))
                query = query.Where(s => s.MovementType == movementType);

            var data = await query
                .OrderByDescending(s => s.MovementDate)
                .Select(s => new
                {
                    s.MovementDate,
                    s.MovementNumber,
                    s.Article.ArticleCode,
                    s.Article.ArticleName,
                    s.MovementType,
                    s.Quantity,
                    s.UnitCost,
                    TotalCost = s.Quantity * s.UnitCost,
                    s.ReferenceNumber,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.WarehouseName : "N/A"
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Date,Reference,SKU,Product Name,Movement Type,Quantity,Unit Cost,Total Cost,Warehouse,PO/Reference");
            foreach (var item in data)
            {
                csv.AppendLine($"{item.MovementDate:yyyy-MM-dd},\"{item.MovementNumber}\",\"{item.ArticleCode}\",\"{item.ArticleName}\",\"{item.MovementType}\",{item.Quantity},{item.UnitCost:F2},{item.TotalCost:F2},\"{item.WarehouseName}\",\"{item.ReferenceNumber}\"");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"StockMovement_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        // POST: Reports/ExportStockValuationToExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportStockValuationToExcel(int? warehouseId = null)
        {
            var query = _context.CurrentStocks
                .Include(cs => cs.Article)
                    .ThenInclude(a => a.ArticleCategoryNavigation)
                .Include(cs => cs.Warehouse)
                .Where(cs => cs.Quantity > 0)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(cs => cs.WarehouseId == warehouseId);

            var valuation = await query
                .GroupBy(cs => new { cs.Article.ArticleCategory, CategoryName = cs.Article.ArticleCategoryNavigation != null ? cs.Article.ArticleCategoryNavigation.Name : "Uncategorized" })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    TotalQuantity = g.Sum(cs => cs.Quantity),
                    TotalValue = g.Sum(cs => cs.Article.StandardCost * cs.Quantity)
                })
                .OrderByDescending(v => v.TotalValue)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Category,Total Quantity,Total Value");
            foreach (var item in valuation)
            {
                csv.AppendLine($"\"{item.CategoryName}\",{item.TotalQuantity:N0},{item.TotalValue:F2}");
            }

            // Add summary row
            var totalValue = valuation.Sum(v => v.TotalValue);
            var totalQuantity = valuation.Sum(v => v.TotalQuantity);
            csv.AppendLine($"\"TOTAL\",{totalQuantity:N0},{totalValue:F2}");

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"StockValuation_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        // GET: Reports/CurrentStock
        [HttpGet]
        public async Task<IActionResult> CurrentStock(int? warehouseId = null, int? categoryId = null)
        {
            var query = _context.CurrentStocks
         .Include(cs => cs.Article)
             .ThenInclude(a => a.ArticleCategoryNavigation)
         .Include(cs => cs.Warehouse)
         .Where(cs => cs.Quantity > 0)
         .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(cs => cs.WarehouseId == warehouseId);

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(cs => cs.Article.ArticleCategory == categoryId);

            var stockItems = await query
                .Select(cs => new CurrentStockReportViewModel
                {
                    ArticleCode = cs.Article.ArticleCode,
                    ArticleName = cs.Article.ArticleName,
                    CategoryName = cs.Article.ArticleCategoryNavigation != null ? cs.Article.ArticleCategoryNavigation.Name : "Uncategorized",
                    WarehouseName = cs.Warehouse.WarehouseName,
                    Quantity = cs.Quantity,
                    UnitCost = cs.Article.StandardCost,  // Use Article's StandardCost
                    TotalValue = cs.Article.StandardCost * cs.Quantity
                })
                .OrderBy(s => s.CategoryName)
                .ThenBy(s => s.ArticleName)
                .ToListAsync();

            ViewBag.Warehouses = await GetWarehouses();
            ViewBag.Categories = await GetCategories();
            ViewBag.SelectedWarehouse = warehouseId;
            ViewBag.SelectedCategory = categoryId;

            return PartialView("_CurrentStockReport", stockItems);
        }

        // GET: Reports/LowStock
        [HttpGet]
        public async Task<IActionResult> LowStock(int? warehouseId = null)
        {
            // Get stock levels from CurrentStock table
            var stockLevels = await _context.CurrentStocks
                .GroupBy(cs => cs.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalStock = g.Sum(cs => cs.Quantity) })
                .ToDictionaryAsync(k => k.ArticleId, v => v.TotalStock);

            var query = _context.Articles
                .Include(a => a.ArticleCategoryNavigation)
                .Where(a => a.IsActive)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
            {
                var stockQuery = _context.CurrentStocks
                    .Where(cs => cs.WarehouseId == warehouseId && cs.Quantity > 0)
                    .Select(cs => cs.ArticleId);
                query = query.Where(a => stockQuery.Contains(a.Id));
            }

            var articles = await query.ToListAsync();

            var lowStockItems = articles
                .Select(a => new LowStockReportViewModel
                {
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.ArticleName,
                    CategoryName = a.ArticleCategoryNavigation != null ? a.ArticleCategoryNavigation.Name : "Uncategorized",
                    CurrentStock = stockLevels.ContainsKey(a.Id) ? (int)stockLevels[a.Id] : 0,
                    ReorderLevel = (int)a.ReorderLevel,
                    Shortage = (int)(a.ReorderLevel - (stockLevels.ContainsKey(a.Id) ? stockLevels[a.Id] : 0)),
                    UnitPrice = a.StandardPrice,
                    PotentialLoss = (a.ReorderLevel - (stockLevels.ContainsKey(a.Id) ? stockLevels[a.Id] : 0)) * a.StandardPrice
                })
                .Where(a => a.CurrentStock <= a.ReorderLevel)
                .OrderByDescending(a => a.Shortage)
                .ToList();

            ViewBag.Warehouses = await GetWarehouses();
            ViewBag.SelectedWarehouse = warehouseId;

            return PartialView("_LowStockReport", lowStockItems);
        }
        // GET: Reports/StockMovement
        [HttpGet]
        public async Task<IActionResult> StockMovement(DateTime? fromDate = null, DateTime? toDate = null,
            int? articleId = null, string? movementType = null)
        {
            if (!fromDate.HasValue)
                fromDate = DateTime.UtcNow.AddMonths(-1);
            if (!toDate.HasValue)
                toDate = DateTime.UtcNow;

            var query = _context.StockMovements
                .Include(s => s.Article)
                .Include(s => s.Warehouse)
                .Where(s => s.MovementDate >= fromDate && s.MovementDate <= toDate)
                .AsQueryable();

            if (articleId.HasValue && articleId > 0)
                query = query.Where(s => s.ArticleId == articleId);

            if (!string.IsNullOrEmpty(movementType))
                query = query.Where(s => s.MovementType == movementType);

            var movements = await query
                .OrderByDescending(s => s.MovementDate)
                .Select(s => new StockMovementReportViewModel
                {
                    MovementDate = s.MovementDate,
                    MovementNumber = s.MovementNumber,
                    ArticleCode = s.Article.ArticleCode,
                    ArticleName = s.Article.ArticleName,
                    MovementType = s.MovementType,
                    Quantity = s.Quantity,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    WarehouseName = s.Warehouse.WarehouseName,
                    ReferenceNumber = s.ReferenceNumber
                })
                .ToListAsync();

            ViewBag.Articles = await GetArticles();
            ViewBag.MovementTypes = GetMovementTypes();
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedArticle = articleId;
            ViewBag.SelectedMovementType = movementType;

            return PartialView("_StockMovementReport", movements);
        }

        // GET: Reports/StockValuation
        [HttpGet]
        public async Task<IActionResult> StockValuation(int? warehouseId = null)
        {
            var query = _context.CurrentStocks
       .Include(cs => cs.Article)
           .ThenInclude(a => a.ArticleCategoryNavigation)
       .Include(cs => cs.Warehouse)
       .Where(cs => cs.Quantity > 0)
       .AsQueryable();

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(cs => cs.WarehouseId == warehouseId);

            var valuation = await query
                .GroupBy(cs => new { cs.Article.ArticleCategory, CategoryName = cs.Article.ArticleCategoryNavigation != null ? cs.Article.ArticleCategoryNavigation.Name : "Uncategorized" })
                .Select(g => new StockValuationCategoryViewModel
                {
                    CategoryName = g.Key.CategoryName,
                    TotalQuantity = g.Sum(cs => cs.Quantity),
                    TotalValue = g.Sum(cs => cs.Article.StandardCost * cs.Quantity)  // Use Article.StandardCost
                })
                .OrderByDescending(v => v.TotalValue)
                .ToListAsync();

            var totalValue = valuation.Sum(v => v.TotalValue);
            var totalQuantity = valuation.Sum(v => v.TotalQuantity);

            ViewBag.TotalValue = totalValue;
            ViewBag.TotalQuantity = totalQuantity;
            ViewBag.Warehouses = await GetWarehouses();
            ViewBag.SelectedWarehouse = warehouseId;

            return PartialView("_StockValuationReport", valuation);
        }

      
        private async Task<List<SelectListItem>> GetWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = w.WarehouseName })
                .ToListAsync();
            warehouses.Insert(0, new SelectListItem { Value = "", Text = "-- All Warehouses --" });
            return warehouses;
        }

        private async Task<List<SelectListItem>> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
            categories.Insert(0, new SelectListItem { Value = "", Text = "-- All Categories --" });
            return categories;
        }

        private async Task<List<SelectListItem>> GetArticles()
        {
            var articles = await _context.Articles
                .Where(a => a.IsActive)
                .OrderBy(a => a.ArticleName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.ArticleCode} - {a.ArticleName}" })
                .ToListAsync();
            articles.Insert(0, new SelectListItem { Value = "", Text = "-- All Products --" });
            return articles;
        }

        private List<SelectListItem> GetMovementTypes()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Types --" },
                new SelectListItem { Value = "Beginning", Text = "Beginning Balance" },
                new SelectListItem { Value = "In", Text = "Goods Receipt" },
                new SelectListItem { Value = "Out", Text = "Sales" },
                new SelectListItem { Value = "Adjustment", Text = "Stock Adjustment" },
                new SelectListItem { Value = "Return", Text = "Return to Supplier" }
            };
        }
    }
}