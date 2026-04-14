using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class BeginningBalanceController : Controller
    {
        private readonly InventoryDbContext _context;

        public BeginningBalanceController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: BeginningBalance
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1, int pageSize = 10)
        {
            var query = _context.BeginningBalances
                .Include(b => b.FiscalPeriod)
                .Include(b => b.CreatedByNavigation)
                .Include(b => b.ApprovedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b => b.BalanceNumber.Contains(searchTerm) ||
                                         (b.Description != null && b.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var balances = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BeginningBalanceListViewModel
                {
                    Id = b.Id,
                    BalanceNumber = b.BalanceNumber,
                    PeriodName = b.FiscalPeriod != null ? b.FiscalPeriod.PeriodName : "",
                    TotalItems = b.BeginningBalanceLines.Count,
                    TotalValue = b.BeginningBalanceLines.Sum(l => l.Quantity * l.UnitCost),
                    Status = b.Status,
                    StatusClass = b.Status == "Draft" ? "warning" : (b.Status == "Approved" ? "info" : "success"),
                    CreatedAt = b.CreatedAt,
                    CreatedByName = b.CreatedByNavigation != null ? b.CreatedByNavigation.UserName : "",
                    ApprovedByName = b.ApprovedByNavigation != null ? b.ApprovedByNavigation.UserName : "",
                    ApprovedAt = b.ApprovedAt,
                    PostedAt = b.PostedAt
                })
                .ToListAsync();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Status --" },
                new SelectListItem { Value = "Draft", Text = "Draft" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Posted", Text = "Posted" }
            };

            var model = new BeginningBalanceIndexViewModel
            {
                Balances = balances,
                SearchTerm = searchTerm ?? string.Empty,
                Status = status,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Statuses = new SelectList(statuses, "Value", "Text", status)
            };

            return View(model);
        }

        // GET: BeginningBalance/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var activePeriod = await _context.FiscalPeriods.FirstOrDefaultAsync(p => !p.IsClosed);
          
            var model = new BeginningBalanceViewModel
            {

                BalanceNumber = await GenerateBalanceNumber(),
                Status = "Draft",
                Lines = new List<BeginningBalanceLineViewModel>
                {
                    new BeginningBalanceLineViewModel() // Empty line for template
                }
            };
            if (activePeriod != null)
            {
                model.FiscalPeriodId = activePeriod.Id;
            }
            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeginningBalanceViewModel model)
        {
            // Remove validation for dropdowns
            ModelState.Remove("FiscalPeriods");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Articles");

            // Filter out empty lines
            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<BeginningBalanceLineViewModel>();

            // Validate at least one valid line
            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item with product, quantity, and unit cost.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // CHECK FOR DUPLICATE PRODUCTS IN THE SAME BALANCE
            var duplicateProducts = validLines
                .GroupBy(l => new { l.ArticleId, WarehouseId = l.WarehouseId ?? model.WarehouseId })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateProducts.Any())
            {
                var duplicateNames = string.Join(", ", duplicateProducts.Select(g =>
                {
                    var article = _context.Articles.Find(g.Key.ArticleId);
                    return $"{article?.ArticleName} (Warehouse: {g.Key.WarehouseId})";
                }));
                ModelState.AddModelError("", $"Duplicate products found: {duplicateNames}. Please combine quantities into one line per product per warehouse.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // COMBINE DUPLICATE LINES IF ANY (safety net)
            var groupedLines = validLines
                .GroupBy(l => new { l.ArticleId, WarehouseId = l.WarehouseId ?? model.WarehouseId, l.BatchNumber, l.SerialNumber })
                .Select(g => new BeginningBalanceLineViewModel
                {
                    ArticleId = g.Key.ArticleId,
                    WarehouseId = g.Key.WarehouseId,
                    Quantity = g.Sum(l => l.Quantity),
                    UnitCost = g.Average(l => l.UnitCost), // Weighted average
                    BatchNumber = g.Key.BatchNumber,
                    SerialNumber = g.Key.SerialNumber,
                    ExpiryDate = g.First().ExpiryDate,
                    Notes = string.Join("; ", g.Select(l => l.Notes).Where(n => !string.IsNullOrEmpty(n)))
                })
                .ToList();

            if (ModelState.IsValid)
            {
                var balance = new BeginningBalance
                {
                    BalanceNumber = model.BalanceNumber,
                    FiscalPeriodId = model.FiscalPeriodId,
                    WarehouseId = model.WarehouseId,
                    Description = model.Description,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.BeginningBalances.Add(balance);
                await _context.SaveChangesAsync();

                // Add grouped lines
                foreach (var line in groupedLines)
                {
                    var balanceLine = new BeginningBalanceLine
                    {
                        BeginningBalanceId = balance.Id,
                        ArticleId = line.ArticleId,
                        WarehouseId = line.WarehouseId ?? model.WarehouseId,
                        Quantity = line.Quantity,
                        UnitCost = line.UnitCost,
                      //  TotalValue = line.Quantity * line.UnitCost,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                        Notes = line.Notes,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };
                    _context.BeginningBalanceLines.Add(balanceLine);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Beginning balance {balance.BalanceNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }
        // GET: BeginningBalance/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var balance = await _context.BeginningBalances
                .Include(b => b.FiscalPeriod)
                .Include(b => b.CreatedByNavigation)
                .Include(b => b.ApprovedByNavigation)
                .Include(b => b.PostedByNavigation)
                .Include(b => b.BeginningBalanceLines)
                    .ThenInclude(l => l.Article)
                .Include(b => b.BeginningBalanceLines)
                    .ThenInclude(l => l.Warehouse)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (balance == null)
            {
                return NotFound();
            }

            var model = new BeginningBalanceDetailsViewModel
            {
                Id = balance.Id,
                BalanceNumber = balance.BalanceNumber,
                FiscalPeriodName = balance.FiscalPeriod?.PeriodName,
                WarehouseName = balance.Warehouse != null ? $"{balance.Warehouse.WarehouseCode} - {balance.Warehouse.WarehouseName}" : "N/A",
                Description = balance.Description,
                Status = balance.Status,
                StatusClass = balance.Status == "Draft" ? "warning" : (balance.Status == "Approved" ? "info" : "success"),
                CreatedAt = balance.CreatedAt,
                CreatedByName = balance.CreatedByNavigation?.UserName,
                ApprovedAt = balance.ApprovedAt,
                ApprovedByName = balance.ApprovedByNavigation?.UserName,
                PostedAt = balance.PostedAt,
                PostedByName = balance.PostedByNavigation?.UserName,
                TotalItems = balance.BeginningBalanceLines.Count,
                TotalQuantity = balance.BeginningBalanceLines.Sum(l => l.Quantity),
                TotalValue = balance.BeginningBalanceLines.Sum(l => l.Quantity * l.UnitCost),
                Lines = balance.BeginningBalanceLines.Select(l => new BeginningBalanceLineDetailsViewModel
                {
                    ArticleCode = l.Article.ArticleCode,
                    ArticleName = l.Article.ArticleName,
                    WarehouseName = l.Warehouse != null ? $"{l.Warehouse.WarehouseCode} - {l.Warehouse.WarehouseName}" : (balance.Warehouse != null ? $"{balance.Warehouse.WarehouseCode} - {balance.Warehouse.WarehouseName}" : "Default"),
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    TotalValue = l.Quantity * l.UnitCost,
                    BatchNumber = l.BatchNumber,
                    SerialNumber = l.SerialNumber,
                    ExpiryDate = l.ExpiryDate.HasValue ? l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Notes = l.Notes
                }).ToList()
            };

            return View(model);
        }

        // GET: BeginningBalance/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var balance = await _context.BeginningBalances
                .Include(b => b.BeginningBalanceLines)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (balance == null || balance.Status != "Draft")
            {
                TempData["ErrorMessage"] = balance == null ? "Balance not found." : "Only draft balances can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var model = new BeginningBalanceViewModel
            {
                Id = balance.Id,
                BalanceNumber = balance.BalanceNumber,
                FiscalPeriodId = balance.FiscalPeriodId,
                WarehouseId = balance.WarehouseId,
                Description = balance.Description,
                Status = balance.Status,
                CreatedAt = balance.CreatedAt,
                Lines = balance.BeginningBalanceLines.Select(l => new BeginningBalanceLineViewModel
                {
                    Id = l.Id,
                    BeginningBalanceId = l.BeginningBalanceId,
                    ArticleId = l.ArticleId,
                    WarehouseId = l.WarehouseId,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    BatchNumber = l.BatchNumber,
                    SerialNumber = l.SerialNumber,
                    ExpiryDate = l.ExpiryDate.HasValue ? l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    Notes = l.Notes
                }).ToList()
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BeginningBalanceViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var balance = await _context.BeginningBalances
                .Include(b => b.BeginningBalanceLines)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (balance == null || balance.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Cannot edit non-draft balance.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("FiscalPeriods");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Articles");

            // Filter out empty lines
            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<BeginningBalanceLineViewModel>();

            // Validate at least one valid line
            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item with product, quantity, and unit cost.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // CHECK FOR DUPLICATE PRODUCTS IN THE SAME BALANCE
            var duplicateProducts = validLines
                .GroupBy(l => new { l.ArticleId, WarehouseId = l.WarehouseId ?? model.WarehouseId })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateProducts.Any())
            {
                var duplicateNames = string.Join(", ", duplicateProducts.Select(g =>
                {
                    var article = _context.Articles.Find(g.Key.ArticleId);
                    return $"{article?.ArticleName} (Warehouse: {g.Key.WarehouseId})";
                }));
                ModelState.AddModelError("", $"Duplicate products found: {duplicateNames}. Please combine quantities into one line per product per warehouse.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // COMBINE DUPLICATE LINES IF ANY
            var groupedLines = validLines
                .GroupBy(l => new { l.ArticleId, WarehouseId = l.WarehouseId ?? model.WarehouseId, l.BatchNumber, l.SerialNumber })
                .Select(g => new BeginningBalanceLineViewModel
                {
                    ArticleId = g.Key.ArticleId,
                    WarehouseId = g.Key.WarehouseId,
                    Quantity = g.Sum(l => l.Quantity),
                    UnitCost = g.Average(l => l.UnitCost),
                    BatchNumber = g.Key.BatchNumber,
                    SerialNumber = g.Key.SerialNumber,
                    ExpiryDate = g.First().ExpiryDate,
                    Notes = string.Join("; ", g.Select(l => l.Notes).Where(n => !string.IsNullOrEmpty(n)))
                })
                .ToList();

            if (ModelState.IsValid)
            {
                // Update balance header
                balance.FiscalPeriodId = model.FiscalPeriodId;
                balance.WarehouseId = model.WarehouseId;
                balance.Description = model.Description;
                balance.UpdatedAt = DateTime.UtcNow;
                balance.UpdatedBy = GetCurrentUserId();

                // Remove old lines
                _context.BeginningBalanceLines.RemoveRange(balance.BeginningBalanceLines);

                // Add new grouped lines
                foreach (var line in groupedLines)
                {
                    var balanceLine = new BeginningBalanceLine
                    {
                        BeginningBalanceId = balance.Id,
                        ArticleId = line.ArticleId,
                        WarehouseId = line.WarehouseId ?? model.WarehouseId,
                        Quantity = line.Quantity,
                        UnitCost = line.UnitCost,
                        //TotalValue = line.Quantity * line.UnitCost,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                        Notes = line.Notes,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };
                    _context.BeginningBalanceLines.Add(balanceLine);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Beginning balance {balance.BalanceNumber} updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }
        // POST: BeginningBalance/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments = null)
        {
            var balance = await _context.BeginningBalances
                .Include(b => b.BeginningBalanceLines)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (balance == null)
            {
                return Json(new { success = false, message = "Balance not found." });
            }

            if (balance.Status != "Draft")
            {
                return Json(new { success = false, message = "Only draft balances can be approved." });
            }

            balance.Status = "Approved";
            balance.ApprovedAt = DateTime.UtcNow;
            balance.ApprovedBy = GetCurrentUserId();
            balance.ApprovalComments = comments;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Balance {balance.BalanceNumber} approved successfully!" });
        }

        // POST: BeginningBalance/Post/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(int id)
        {
            var balance = await _context.BeginningBalances
                .Include(b => b.BeginningBalanceLines)
                .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (balance.Status == "Posted")
            {
                return Json(new { success = false, message = "This balance has already been posted." });
            }
            if (balance == null)
            {
                return Json(new { success = false, message = "Balance not found." });
            }

            if (balance.Status != "Approved")
            {
                return Json(new { success = false, message = "Only approved balances can be posted." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var line in balance.BeginningBalanceLines)
                {
                    // CHECK FOR EXISTING CURRENT STOCK FIRST
                    var existingStock = await _context.CurrentStocks
                        .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                                   cs.WarehouseId == (line.WarehouseId ?? balance.WarehouseId) &&
                                                   cs.BatchNumber == line.BatchNumber &&
                                                   cs.SerialNumber == line.SerialNumber);

                    if (existingStock != null)
                    {
                        // UPDATE existing record
                        existingStock.Quantity += line.Quantity;
                        existingStock.LastUpdated = DateTime.UtcNow;
                        _context.CurrentStocks.Update(existingStock);
                    }
                    else
                    {
                        // INSERT new record
                        var currentStock = new CurrentStock
                        {
                            ArticleId = line.ArticleId,
                            WarehouseId = line.WarehouseId ?? balance.WarehouseId ?? 1,
                            Quantity = line.Quantity,
                            ReservedQuantity = 0,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            ExpiryDate = line.ExpiryDate,
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.CurrentStocks.Add(currentStock);
                    }
                    //var voucher = new Voucher
                    //{
                    //    VoucherNumber = $"BB-{balance.BalanceNumber}",
                    //    VoucherType = "BeginningBalance",
                    //    VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    //    PostingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    //    Status = "Posted",
                    //    CreatedAt = DateTime.UtcNow,
                    //    CreatedBy = GetCurrentUserId()
                    //};
                    //_context.Vouchers.Add(voucher);
                    //await _context.SaveChangesAsync();

                    // Create stock movement record
                    var stockMovement = new StockMovement
                    {
                        MovementNumber = $"BB-{balance.BalanceNumber}",
                        ArticleId = line.ArticleId,
                        WarehouseId = line.WarehouseId ?? balance.WarehouseId ?? 1,
                        MovementType = "Beginning",
                        ActivityType = "Beginning Balance",
                        Quantity = line.Quantity,
                        PreviousStock = existingStock?.Quantity ?? 0,
                        NewStock = (existingStock?.Quantity ?? 0) + line.Quantity,
                        UnitCost = line.UnitCost,
                        TotalCost = line.Quantity * line.UnitCost,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate,
                        MovementDate = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        ReferenceNumber = balance.BalanceNumber,
                        VoucherId = null,
                        VoucherLineId=null
                    };
                    _context.StockMovements.Add(stockMovement);
                }

                balance.Status = "Posted";
                balance.PostedAt = DateTime.UtcNow;
                balance.PostedBy = GetCurrentUserId();
                balance.UpdatedAt = DateTime.UtcNow;

                _context.Update(balance);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Balance {balance.BalanceNumber} posted to inventory successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Error posting balance: {ex.Message}" });
            }
        }

        // POST: BeginningBalance/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var balance = await _context.BeginningBalances
                .Include(b => b.BeginningBalanceLines)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (balance == null)
            {
                return Json(new { success = false, message = "Balance not found." });
            }

            if (balance.Status != "Draft")
            {
                return Json(new { success = false, message = "Only draft balances can be deleted." });
            }

            _context.BeginningBalanceLines.RemoveRange(balance.BeginningBalanceLines);
            _context.BeginningBalances.Remove(balance);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Balance {balance.BalanceNumber} deleted successfully!" });
        }

        // GET: BeginningBalance/GetProductDetails
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int articleId)
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == articleId && a.IsActive);

            if (article == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                articleCode = article.ArticleCode,
                articleName = article.ArticleName,
                standardCost = article.StandardCost,
                isBatchTracked = article.IsBatchTracked,
                isSerialized = article.IsSerialized,
                isExpiryTracked = article.IsExpiryTracked
            });
        }

        // Helper: Generate balance number
        private async Task<string> GenerateBalanceNumber()
        {
            string prefix = "BB-";
            var lastBalance = await _context.BeginningBalances
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastBalance != null && lastBalance.BalanceNumber.StartsWith(prefix))
            {
                var match = Regex.Match(lastBalance.BalanceNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D6}";
        }

        // Helper: Populate dropdowns
        private async Task PopulateDropdowns(BeginningBalanceViewModel model)
        {
            // Fiscal Periods (only open periods)
            var periods = await _context.FiscalPeriods
                .Where(p => !p.IsClosed)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.PeriodName} ({p.StartDate:MMM dd, yyyy} - {p.EndDate:MMM dd, yyyy})" })
                .ToListAsync();
            model.FiscalPeriods = new SelectList(periods, "Value", "Text", model.FiscalPeriodId);

            // Warehouses
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = $"{w.WarehouseCode} - {w.WarehouseName}" })
                .ToListAsync();
            warehouses.Insert(0, new SelectListItem { Value = "", Text = "-- Select Warehouse --" });
            model.Warehouses = new SelectList(warehouses, "Value", "Text", model.WarehouseId?.ToString());

            // Articles (Products)
            var articles = await _context.Articles
                .Where(a => a.IsActive && a.IsStockable)
                .OrderBy(a => a.ArticleName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.ArticleCode} - {a.ArticleName}" })
                .ToListAsync();
            model.Articles = new SelectList(articles, "Value", "Text");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return 1;
        }
    }
}