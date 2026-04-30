using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.DTOs;
using Store_Management_System.Extensions;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class BeginningBalanceController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public BeginningBalanceController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
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
                var balanceDto = balance.ToDto();
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Create", "BeginningBalance", balance.Id, null,
    JsonSerializer.Serialize(balanceDto), $"Created beginning balance: {balanceDto.BalanceNumber}");

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
                var balanceDto = balance.ToDto();
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Edit", "BeginningBalance", balance.Id,
                JsonSerializer.Serialize(balanceDto), JsonSerializer.Serialize(balanceDto),
                $"Updated beginning balance: {balanceDto.BalanceNumber}");
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
            var balanceDto = balance.ToDto();
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("Approve", "BeginningBalance", balance.Id,
                JsonSerializer.Serialize(balanceDto), JsonSerializer.Serialize(balanceDto),
                $"Approved beginning balance: {balance.BalanceNumber}");

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

            if (balance == null)
            {
                return Json(new { success = false, message = "Balance not found." });
            }

            if (balance.Status == "Posted")
            {
                return Json(new { success = false, message = "This balance has already been posted." });
            }

            if (balance.Status != "Approved")
            {
                return Json(new { success = false, message = "Only approved balances can be posted." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Use a counter to generate unique movement numbers
                int lineCounter = 0;

                foreach (var line in balance.BeginningBalanceLines)
                {
                    lineCounter++;

                    // CHECK FOR EXISTING CURRENT STOCK FIRST
                    var existingStock = await _context.CurrentStocks
                        .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                                   cs.WarehouseId == (line.WarehouseId ?? balance.WarehouseId) &&
                                                   cs.BatchNumber == line.BatchNumber &&
                                                   cs.SerialNumber == line.SerialNumber);

                    if (existingStock != null)
                    {
                        existingStock.Quantity += line.Quantity;
                        existingStock.LastUpdated = DateTime.UtcNow;
                        _context.CurrentStocks.Update(existingStock);
                    }
                    else
                    {
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

                    string cleanNumber = balance.BalanceNumber;
                    if (cleanNumber.StartsWith("BB-"))
                    {
                        cleanNumber = cleanNumber.Substring(3); // Remove "BB-"
                    }

                    // Build unique number: BB-XXXXXX-L####-######
                    var movementNumber = $"BB-{cleanNumber}-L{lineCounter:D4}-{DateTime.UtcNow.Ticks % 1000000:D6}";

                    var stockMovement = new StockMovement
                    {
                        MovementNumber = movementNumber,
                        ArticleId = line.ArticleId,
                        WarehouseId = line.WarehouseId ?? balance.WarehouseId ?? 1,
                        MovementType = "In",
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
                        VoucherLineId = null
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

                var balanceDto = balance.ToDto();
                await _activityLogService.LogAsync("Post", "BeginningBalance", balance.Id,
                    JsonSerializer.Serialize(balanceDto),
                    JsonSerializer.Serialize(balanceDto),
                    $"Posted beginning balance: {balance.BalanceNumber}");

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
            var balanceDto = balance.ToDto();
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("Delete", "BeginningBalance", id, null, null,
            $"Deleted beginning balance: {balanceDto.BalanceNumber}");
            return Json(new { success = true, message = $"Balance {balanceDto.BalanceNumber} deleted successfully!" });
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
        // GET: BeginningBalance/Import
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            await PopulateImportDropdowns();
            return View();
        }

        // POST: BeginningBalance/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, int fiscalPeriodId, string? description = null)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                await PopulateImportDropdowns();
                return View();
            }

            if (fiscalPeriodId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a fiscal period.";
                await PopulateImportDropdowns();
                return View();
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
            {
                TempData["ErrorMessage"] = "Please upload a CSV or Excel (.xlsx/.xls) file.";
                await PopulateImportDropdowns();
                return View();
            }

            try
            {
                List<BeginningBalanceImportDto> records;

                if (extension == ".csv")
                {
                    records = await ParseCsvFile(file);
                }
                else
                {
                    records = await ParseExcelFile(file);
                }

                if (records.Count == 0)
                {
                    TempData["ErrorMessage"] = "No valid records found in the file. Please check the format.";
                    await PopulateImportDropdowns();
                    return View();
                }

                // Validate and import
                var result = await ImportRecords(records, fiscalPeriodId, description);

                if (result.SuccessCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {result.SuccessCount} lines to Balance {result.BalanceNumber}!";
                }

                if (result.FailureCount > 0)
                {
                    TempData["ErrorMessage"] = $"{result.FailureCount} records failed to import. Check the error details.";
                }

                return View("ImportResult", result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing file: {ex.Message}";
                await PopulateImportDropdowns();
                return View();
            }
        }

        // POST: BeginningBalance/PreviewImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewImport(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a file to upload." });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
            {
                return Json(new { success = false, message = "Please upload a CSV or Excel file." });
            }

            try
            {
                List<BeginningBalanceImportDto> records;

                if (extension == ".csv")
                {
                    records = await ParseCsvFile(file);
                }
                else
                {
                    records = await ParseExcelFile(file);
                }

                // Preview first 10 rows
                var preview = records.Take(10).Select((r, i) => new
                {
                    Row = i + 1,
                    r.ArticleCode,
                    r.ArticleName,
                    r.WarehouseCode,
                    r.Quantity,
                    r.UnitCost,
                    TotalValue = r.Quantity * r.UnitCost,
                    r.BatchNumber,
                    r.ExpiryDate,
                    IsValid = (!string.IsNullOrWhiteSpace(r.ArticleCode) || !string.IsNullOrWhiteSpace(r.ArticleName))
                              && r.Quantity > 0
                              && !string.IsNullOrWhiteSpace(r.WarehouseCode)
                }).ToList();

                return Json(new
                {
                    success = true,
                    totalRows = records.Count,
                    preview = preview,
                    previewCount = preview.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error reading file: {ex.Message}" });
            }
        }

        // GET: BeginningBalance/DownloadSample
        [HttpGet]
        public IActionResult DownloadSample()
        {
            var csvContent = "Article Code,Article Name,Warehouse Code,Quantity,Unit Cost,Batch Number,Serial Number,Expiry Date,Notes\n";
            csvContent += "ART-2704-0001,,WH-001,100,15.50,BATCH-001,,2026-12-31,\n";
            csvContent += ",Wireless Mouse,WH-001,50,12.00,BATCH-002,SN-001,,Initial stock\n";
            csvContent += "ART-2704-0002,,WH-002,200,8.75,,,2026-06-30,Supplier: ABC Corp\n";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "beginning_balance_import_template.csv");
        }

        // ==========================================
        // IMPORT HELPER METHODS
        // ==========================================

        private async Task<List<BeginningBalanceImportDto>> ParseCsvFile(IFormFile file)
        {
            var records = new List<BeginningBalanceImportDto>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(headerLine))
                return records;

            var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"').ToLower()).ToArray();

            // Map column indices
            var codeIndex = Array.FindIndex(headers, h => h.Contains("code") && !h.Contains("warehouse"));
            var nameIndex = Array.FindIndex(headers, h => h.Contains("name") && !h.Contains("warehouse"));
            var whIndex = Array.FindIndex(headers, h => h.Contains("warehouse"));
            var qtyIndex = Array.FindIndex(headers, h => h.Contains("qty") || h.Contains("quantity") || h.Contains("qunantity"));
            var costIndex = Array.FindIndex(headers, h => h.Contains("cost") || h.Contains("price"));
            var batchIndex = Array.FindIndex(headers, h => h.Contains("batch") || h.Contains("lot"));
            var serialIndex = Array.FindIndex(headers, h => h.Contains("serial") || h.Contains("sn"));
            var expiryIndex = Array.FindIndex(headers, h => h.Contains("expir") || h.Contains("exp") || h.Contains("date"));
            var notesIndex = Array.FindIndex(headers, h => h.Contains("note") || h.Contains("remark") || h.Contains("comment"));

            // If no quantity column found, try alternate names
            if (qtyIndex < 0)
            {
                qtyIndex = Array.FindIndex(headers, h => h == "qty" || h == "quantity");
            }

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = ParseCsvLine(line);

                // Skip empty rows
                if (values.All(v => string.IsNullOrWhiteSpace(v))) continue;

                var record = new BeginningBalanceImportDto
                {
                    ArticleCode = codeIndex >= 0 && codeIndex < values.Count ? values[codeIndex].Trim() : "",
                    ArticleName = nameIndex >= 0 && nameIndex < values.Count ? values[nameIndex].Trim() : "",
                    WarehouseCode = whIndex >= 0 && whIndex < values.Count ? values[whIndex].Trim() : "",
                    Quantity = qtyIndex >= 0 && qtyIndex < values.Count ? ParseDecimal(values[qtyIndex]) : 0,
                    UnitCost = costIndex >= 0 && costIndex < values.Count ? ParseDecimal(values[costIndex]) : 0,
                    BatchNumber = batchIndex >= 0 && batchIndex < values.Count ? values[batchIndex].Trim() : null,
                    SerialNumber = serialIndex >= 0 && serialIndex < values.Count ? values[serialIndex].Trim() : null,
                    ExpiryDate = expiryIndex >= 0 && expiryIndex < values.Count ? values[expiryIndex].Trim() : null,
                    Notes = notesIndex >= 0 && notesIndex < values.Count ? values[notesIndex].Trim() : null
                };

                // Only add if there's some meaningful data
                if (!string.IsNullOrWhiteSpace(record.ArticleCode) || !string.IsNullOrWhiteSpace(record.ArticleName))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        private async Task<List<BeginningBalanceImportDto>> ParseExcelFile(IFormFile file)
        {
            // For now, treat Excel as CSV
            // For proper Excel support: install EPPlus NuGet package
            try
            {
                return await ParseCsvFile(file);
            }
            catch
            {
                throw new Exception("Excel parsing requires the EPPlus package. Please save your file as CSV or install EPPlus.");
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim('"').Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            values.Add(currentValue.Trim('"').Trim());

            return values;
        }

        private decimal ParseDecimal(string value)
        {
            value = value.Replace("$", "").Replace(",", "").Replace(" ", "").Trim();
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        private async Task<BeginningBalanceImportResult> ImportRecords(
            List<BeginningBalanceImportDto> records,
            int fiscalPeriodId,
            string? description)
        {
            var result = new BeginningBalanceImportResult
            {
                TotalRows = records.Count,
                Errors = new List<string>()
            };

            // Pre-load lookup data
            var articles = await _context.Articles
                .Where(a => a.IsActive && a.IsStockable)
                .Select(a => new { a.Id, a.ArticleCode, a.ArticleName, a.IsBatchTracked, a.IsSerialized, a.IsExpiryTracked })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new { w.Id, w.WarehouseCode, w.WarehouseName })
                .ToListAsync();

            var fiscalPeriod = await _context.FiscalPeriods.FindAsync(fiscalPeriodId);
            if (fiscalPeriod == null)
            {
                result.Errors.Add("Invalid fiscal period selected.");
                result.FailureCount = records.Count;
                return result;
            }

            // Create the Beginning Balance header
            var balance = new BeginningBalance
            {
                BalanceNumber = await GenerateBalanceNumber(),
                FiscalPeriodId = fiscalPeriodId,
                Description = description ?? $"Imported from file - {DateTime.Now:yyyy-MM-dd HH:mm}",
                Status = "Draft",
                BalanceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetCurrentUserId()
            };

            _context.BeginningBalances.Add(balance);
            await _context.SaveChangesAsync();

            result.BalanceId = balance.Id;
            result.BalanceNumber = balance.BalanceNumber;

            var rowNumber = 0;
            var lineNumber = 0;

            foreach (var record in records)
            {
                rowNumber++;
                try
                {
                    // Validate
                    if (string.IsNullOrWhiteSpace(record.ArticleCode) && string.IsNullOrWhiteSpace(record.ArticleName))
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Either Article Code or Article Name is required.");
                        continue;
                    }

                    if (record.Quantity <= 0)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Quantity must be greater than 0.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.WarehouseCode))
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Warehouse Code is required.");
                        continue;
                    }

                    // Find article by code or name
                    var article = articles.FirstOrDefault(a =>
                        (!string.IsNullOrWhiteSpace(record.ArticleCode) &&
                         a.ArticleCode.Equals(record.ArticleCode, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(record.ArticleName) &&
                         a.ArticleName.Equals(record.ArticleName, StringComparison.OrdinalIgnoreCase)));

                    if (article == null)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Article '{record.ArticleCode ?? record.ArticleName}' not found.");
                        continue;
                    }

                    // Find warehouse by code or name
                    var warehouse = warehouses.FirstOrDefault(w =>
                        w.WarehouseCode.Equals(record.WarehouseCode, StringComparison.OrdinalIgnoreCase) ||
                        w.WarehouseName.Equals(record.WarehouseCode, StringComparison.OrdinalIgnoreCase));

                    if (warehouse == null)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Warehouse '{record.WarehouseCode}' not found.");
                        continue;
                    }

                    // Parse expiry date if provided
                    DateOnly? expiryDate = null;
                    if (!string.IsNullOrWhiteSpace(record.ExpiryDate))
                    {
                        if (DateOnly.TryParse(record.ExpiryDate, out var parsedDate))
                        {
                            expiryDate = parsedDate;
                        }
                        else if (DateTime.TryParse(record.ExpiryDate, out var parsedDateTime))
                        {
                            expiryDate = DateOnly.FromDateTime(parsedDateTime);
                        }
                    }

                    // Create the balance line
                    var balanceLine = new BeginningBalanceLine
                    {
                        BeginningBalanceId = balance.Id,
                        ArticleId = article.Id,
                        WarehouseId = warehouse.Id,
                        Quantity = record.Quantity,
                        UnitCost = record.UnitCost,
                        BatchNumber = string.IsNullOrWhiteSpace(record.BatchNumber) ? null : record.BatchNumber.Trim(),
                        SerialNumber = string.IsNullOrWhiteSpace(record.SerialNumber) ? null : record.SerialNumber.Trim(),
                        ExpiryDate = expiryDate,
                        Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.BeginningBalanceLines.Add(balanceLine);
                    await _context.SaveChangesAsync();
                    result.SuccessCount++;
                    lineNumber++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            // If no success lines, delete the empty balance
            if (result.SuccessCount == 0)
            {
                _context.BeginningBalances.Remove(balance);
                await _context.SaveChangesAsync();
                result.BalanceId = 0;
                result.BalanceNumber = "";
                result.Errors.Insert(0, "No valid records were imported. The balance was not created.");
            }
            else
            {
                // Update the balance with warehouse if all lines are for one warehouse
                var distinctWarehouses = await _context.BeginningBalanceLines
                    .Where(l => l.BeginningBalanceId == balance.Id)
                    .Select(l => l.WarehouseId)
                    .Distinct()
                    .ToListAsync();

                if (distinctWarehouses.Count == 1)
                {
                    balance.WarehouseId = distinctWarehouses[0];
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync("Create", "BeginningBalance", balance.Id, null,
                    JsonSerializer.Serialize(new { balance.BalanceNumber, LineCount = result.SuccessCount }),
                    $"Imported beginning balance: {balance.BalanceNumber} with {result.SuccessCount} lines");
            }

            return result;
        }

        private async Task PopulateImportDropdowns()
        {
            // Fiscal Periods (only open periods)
            var periods = await _context.FiscalPeriods
                .Where(p => !p.IsClosed)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.PeriodName} ({p.StartDate:MMM dd, yyyy} - {p.EndDate:MMM dd, yyyy})"
                })
                .ToListAsync();

            ViewBag.FiscalPeriods = new SelectList(periods, "Value", "Text");

            // Pre-select the current active period
            var activePeriod = await _context.FiscalPeriods.FirstOrDefaultAsync(p => !p.IsClosed);
            ViewBag.DefaultFiscalPeriodId = activePeriod?.Id ?? 0;
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