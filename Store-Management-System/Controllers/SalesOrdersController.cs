using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class SalesOrdersController : Controller
    {
        private readonly InventoryDbContext _context;

        public SalesOrdersController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: SalesOrders
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Consignee)
                .Where(v => v.VoucherType == "SO" && !v.IsReturn)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Consignee != null && v.Consignee.ConsigneeName.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var salesOrders = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new SalesOrderListViewModel
                {
                    Id = v.Id,
                    VoucherNumber = v.VoucherNumber,
                    CustomerName = v.Consignee != null ? v.Consignee.ConsigneeName : "",
                    VoucherDate = v.VoucherDate,
                    DeliveryDate = v.DeliveryDate,
                    TotalAmount = v.TotalAmount,
                    Status = v.Status,
                    StatusClass = v.Status == "Draft" ? "warning" :
                                  v.Status == "Approved" ? "info" :
                                  v.Status == "Shipped" ? "primary" :
                                  v.Status == "Completed" ? "success" : "danger",
                    ItemCount = v.VoucherLines.Count,
                    ShippedCount = (int)v.VoucherLines.Sum(l => l.ShippedQuantity ?? 0)
                })
                .ToListAsync();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Status --" },
                new SelectListItem { Value = "Draft", Text = "Draft" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Shipped", Text = "Shipped" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            var model = new SalesOrderIndexViewModel
            {
                SalesOrders = salesOrders,
                SearchTerm = searchTerm ?? string.Empty,
                Status = status,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Statuses = new SelectList(statuses, "Value", "Text", status)
            };

            return View(model);
        }

        // GET: SalesOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Consignee)
                .Include(v => v.Warehouse)
                .Include(v => v.ApprovedByNavigation)
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // GET: SalesOrders/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new SalesOrderViewModel
            {
                VoucherNumber = await GenerateVoucherNumber("SO"),
                Status = "Draft",
                DeliveryDate = DateTime.UtcNow.AddDays(7),
                Lines = new List<SalesOrderLineViewModel> { new SalesOrderLineViewModel() }
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: SalesOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesOrderViewModel model)
        {
            ModelState.Remove("Customers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<SalesOrderLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // Check stock availability
            foreach (var line in validLines)
            {
                var stock = await GetAvailableStock(line.ArticleId, model.WarehouseId);
                line.AvailableStock = stock;
                if (stock < line.Quantity)
                {
                    ModelState.AddModelError("", $"Insufficient stock for {line.ArticleName}. Available: {stock}, Requested: {line.Quantity}");
                }
            }

            // Check for duplicate products
            var duplicates = validLines.GroupBy(l => l.ArticleId).Where(g => g.Count() > 1).ToList();
            if (duplicates.Any())
            {
                ModelState.AddModelError("", "Duplicate products found. Please combine quantities.");
                await PopulateDropdowns(model);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var voucher = new Voucher
                {
                    VoucherNumber = model.VoucherNumber,
                    VoucherType = "SO",
                    ActivityId = 2, // Sales activity
                    ConsigneeId = model.CustomerId,
                    WarehouseId = model.WarehouseId,
                    VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PostingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DeliveryDate = model.DeliveryDate.HasValue ? DateOnly.FromDateTime(model.DeliveryDate.Value) : null,
                    ShippingAddress = model.ShippingAddress,
                    BillingAddress = model.BillingAddress,
                    SubTotal = model.SubTotal,
                    DiscountAmount = model.DiscountAmount ?? 0,
                    TaxAmount = model.TaxAmount,
                    ShippingCharge = model.ShippingCharge ?? 0,
                    TotalAmount = model.TotalAmount,
                    Status = model.Status,
                    Remarks = model.Remarks,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                // Get the article to find its unit
                var article = await _context.Articles.FindAsync(validLines.First().ArticleId);

                // Add lines
                foreach (var line in validLines)
                {
                    var voucherLine = new VoucherLine
                    {
                        VoucherId = voucher.Id,
                        ArticleId = line.ArticleId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        UnitId = article?.BaseUnitId ?? 1,
                        WarehouseId = model.WarehouseId,
                        DiscountPercent = line.DiscountPercent,
                        LineTotal = line.LineTotal,
                        Description = line.Description,
                        ShippedQuantity = 0
                    };
                    _context.VoucherLines.Add(voucherLine);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Sales Order {voucher.VoucherNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: SalesOrders/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null || voucher.Status != "Draft")
            {
                TempData["ErrorMessage"] = voucher == null ? "Sales Order not found." : "Only draft orders can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var model = new SalesOrderViewModel
            {
                Id = voucher.Id,
                VoucherNumber = voucher.VoucherNumber,
                CustomerId = voucher.ConsigneeId ?? 0,
                WarehouseId = voucher.WarehouseId ?? 0,
                DeliveryDate = voucher.DeliveryDate.HasValue ? voucher.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                ShippingAddress = voucher.ShippingAddress,
                BillingAddress = voucher.BillingAddress,
                Remarks = voucher.Remarks,
                Status = voucher.Status,
                DiscountAmount = voucher.DiscountAmount,
                ShippingCharge = voucher.ShippingCharge,
                TaxRate = 0,
                Lines = voucher.VoucherLines.Select(l => new SalesOrderLineViewModel
                {
                    Id = l.Id,
                    ArticleId = l.ArticleId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent,
                    Description = l.Description,
                    QuantityShipped = l.ShippedQuantity ?? 0
                }).ToList()
            };

            // Get available stock for each line
            foreach (var line in model.Lines)
            {
                line.AvailableStock = await GetAvailableStock(line.ArticleId, model.WarehouseId);
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: SalesOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SalesOrderViewModel model)
        {
            if (id != model.Id) return NotFound();

            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null || voucher.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Cannot edit non-draft order.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Customers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<SalesOrderLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // Check stock availability
            foreach (var line in validLines)
            {
                var stock = await GetAvailableStock(line.ArticleId, model.WarehouseId);
                line.AvailableStock = stock;
                if (stock < line.Quantity)
                {
                    ModelState.AddModelError("", $"Insufficient stock for {line.ArticleName}. Available: {stock}, Requested: {line.Quantity}");
                }
            }

            if (ModelState.IsValid)
            {
                // Update header
                voucher.ConsigneeId = model.CustomerId;
                voucher.WarehouseId = model.WarehouseId;
                voucher.DeliveryDate = model.DeliveryDate.HasValue ? DateOnly.FromDateTime(model.DeliveryDate.Value) : null;
                voucher.ShippingAddress = model.ShippingAddress;
                voucher.BillingAddress = model.BillingAddress;
                voucher.Remarks = model.Remarks;
                voucher.SubTotal = model.SubTotal;
                voucher.DiscountAmount = model.DiscountAmount ?? 0;
                voucher.ShippingCharge = model.ShippingCharge ?? 0;
                voucher.TotalAmount = model.TotalAmount;
                voucher.UpdatedAt = DateTime.UtcNow;
                voucher.UpdatedBy = GetCurrentUserId();

                // Remove old lines
                _context.VoucherLines.RemoveRange(voucher.VoucherLines);
                var article = await _context.Articles.FindAsync(validLines.First().ArticleId);

                // Add new lines
                foreach (var line in validLines)
                {
                    var voucherLine = new VoucherLine
                    {
                        VoucherId = voucher.Id,
                        ArticleId = line.ArticleId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        UnitId = article?.BaseUnitId ?? 1,
                        WarehouseId = model.WarehouseId,
                        DiscountPercent = line.DiscountPercent,
                        LineTotal = line.LineTotal,
                        Description = line.Description,
                        ShippedQuantity = 0
                    };
                    _context.VoucherLines.Add(voucherLine);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Sales Order {voucher.VoucherNumber} updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: SalesOrders/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments = null)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null)
                return Json(new { success = false, message = "Sales Order not found." });

            if (voucher.Status != "Draft")
                return Json(new { success = false, message = "Only draft orders can be approved." });

            // Verify stock before approval
            foreach (var line in voucher.VoucherLines)
            {
                var stock = await GetAvailableStock(line.ArticleId, voucher.WarehouseId ?? 1);
                if (stock < line.Quantity)
                {
                    return Json(new { success = false, message = $"Insufficient stock for {line.Article?.ArticleName}. Available: {stock}, Ordered: {line.Quantity}" });
                }
            }

            // Reserve stock
            foreach (var line in voucher.VoucherLines)
            {
                await ReserveStock(line.ArticleId, voucher.WarehouseId ?? 1, line.Quantity);
            }

            voucher.Status = "Approved";
            voucher.ApprovedAt = DateTime.UtcNow;
            voucher.ApprovedBy = GetCurrentUserId();
            voucher.ApprovalComments = comments;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Sales Order {voucher.VoucherNumber} approved and stock reserved!" });
        }

        // POST: SalesOrders/Ship/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ship(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null)
                return Json(new { success = false, message = "Sales Order not found." });

            if (voucher.Status != "Approved")
                return Json(new { success = false, message = "Only approved orders can be shipped." });

            voucher.Status = "Shipped";
            voucher.ShippedDate = DateTime.UtcNow;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Sales Order {voucher.VoucherNumber} marked as shipped!" });
        }

        // POST: SalesOrders/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null)
                return Json(new { success = false, message = "Sales Order not found." });

            if (voucher.Status != "Shipped")
                return Json(new { success = false, message = "Only shipped orders can be completed." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Update stock (remove from reserved, add to sold)
                foreach (var line in voucher.VoucherLines)
                {
                    await ReleaseReservedStock(line.ArticleId, voucher.WarehouseId ?? 1, line.Quantity);
                    await UpdateSoldStock(line.ArticleId, voucher.WarehouseId ?? 1, line.Quantity);

                    // Update shipped quantity
                    line.ShippedQuantity = line.Quantity;

                    // Create stock movement
                    var stockMovement = new StockMovement
                    {
                        MovementNumber = $"SO-{voucher.VoucherNumber}",
                        ArticleId = line.ArticleId,
                        WarehouseId = voucher.WarehouseId ?? 1,
                        MovementType = "Out",
                        ActivityType = "Sales",
                        Quantity = -line.Quantity,
                        UnitCost = line.UnitPrice,
                        TotalCost = -line.LineTotal,
                        MovementDate = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        ReferenceNumber = voucher.VoucherNumber,
                        VoucherId = voucher.Id
                    };
                    _context.StockMovements.Add(stockMovement);
                }

                voucher.Status = "Completed";
                voucher.CompletedDate = DateTime.UtcNow;
                voucher.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Sales Order {voucher.VoucherNumber} completed successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: SalesOrders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason = null)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "SO");

            if (voucher == null)
                return Json(new { success = false, message = "Sales Order not found." });

            if (voucher.Status != "Draft" && voucher.Status != "Approved")
                return Json(new { success = false, message = "This order cannot be cancelled." });

            // Release reserved stock if approved
            if (voucher.Status == "Approved")
            {
                foreach (var line in voucher.VoucherLines)
                {
                    await ReleaseReservedStock(line.ArticleId, voucher.WarehouseId ?? 1, line.Quantity);
                }
            }

            voucher.Status = "Cancelled";
            voucher.Remarks = (voucher.Remarks != null ? voucher.Remarks + " | " : "") + $"Cancelled: {reason}";
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Sales Order {voucher.VoucherNumber} cancelled!" });
        }

        // Helper: Get available stock
        private async Task<decimal> GetAvailableStock(int articleId, int warehouseId)
        {
            var stock = await _context.CurrentStocks
                .Where(cs => cs.ArticleId == articleId && cs.WarehouseId == warehouseId)
                .SumAsync(cs => cs.Quantity - cs.ReservedQuantity);
            return stock;
        }

        // Helper: Reserve stock
        private async Task ReserveStock(int articleId, int warehouseId, decimal quantity)
        {
            var stock = await _context.CurrentStocks
                .FirstOrDefaultAsync(cs => cs.ArticleId == articleId && cs.WarehouseId == warehouseId);

            if (stock != null)
            {
                stock.ReservedQuantity += quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
        }

        // Helper: Release reserved stock
        private async Task ReleaseReservedStock(int articleId, int warehouseId, decimal quantity)
        {
            var stock = await _context.CurrentStocks
                .FirstOrDefaultAsync(cs => cs.ArticleId == articleId && cs.WarehouseId == warehouseId);

            if (stock != null)
            {
                stock.ReservedQuantity -= quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
        }

        // Helper: Update sold stock
        private async Task UpdateSoldStock(int articleId, int warehouseId, decimal quantity)
        {
            var stock = await _context.CurrentStocks
                .FirstOrDefaultAsync(cs => cs.ArticleId == articleId && cs.WarehouseId == warehouseId);

            if (stock != null)
            {
                stock.Quantity -= quantity;
                stock.ReservedQuantity -= quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
        }

        // Helper: Generate voucher number
        private async Task<string> GenerateVoucherNumber(string prefix)
        {
            var lastVoucher = await _context.Vouchers
                .Where(v => v.VoucherNumber.StartsWith(prefix))
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastVoucher != null)
            {
                var match = Regex.Match(lastVoucher.VoucherNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"{prefix}-{nextNumber:D6}";
        }

        // Helper: Populate dropdowns
        private async Task PopulateDropdowns(SalesOrderViewModel model)
        {
            var customers = await _context.Consignees
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ConsigneeName })
                .ToListAsync();
            model.Customers = new SelectList(customers, "Value", "Text", model.CustomerId);

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = $"{w.WarehouseCode} - {w.WarehouseName}" })
                .ToListAsync();
            model.Warehouses = new SelectList(warehouses, "Value", "Text", model.WarehouseId);

            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .OrderBy(a => a.ArticleName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.ArticleCode} - {a.ArticleName}" })
                .ToListAsync();
            model.Products = new SelectList(products, "Value", "Text");
        }

        // Helper: Get current user ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}