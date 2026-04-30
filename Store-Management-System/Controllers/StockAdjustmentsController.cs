using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class StockAdjustmentsController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;

        public StockAdjustmentsController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: StockAdjustments
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", int? warehouseId = null, int page = 1, int pageSize = 10)
        {
            var query = _context.StockAdjustments
                .Include(s => s.Warehouse)
                .Include(s => s.CreatedByNavigation)
                .Include(s => s.ApprovedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s => s.AdjustmentNumber.Contains(searchTerm) ||
                                         (s.Notes != null && s.Notes.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (warehouseId.HasValue && warehouseId > 0)
            {
                query = query.Where(s => s.WarehouseId == warehouseId);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var adjustments = await query
                .OrderByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StockAdjustmentListViewModel
                {
                    Id = s.Id,
                    AdjustmentNumber = s.AdjustmentNumber,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.WarehouseName : "",
                    AdjustmentDate = s.AdjustmentDate,
                    AdjustmentType = s.AdjustmentType,
                    ReasonCategory = s.ReasonCategory,
                    TotalValue = s.TotalValue,
                    Status = s.Status,
                    StatusClass = s.Status == "Draft" ? "warning" :
                                  s.Status == "Submitted" ? "info" :
                                  s.Status == "Approved" ? "primary" :
                                  s.Status == "Posted" ? "success" : "secondary",
                    LineCount = s.StockAdjustmentLines.Count,
                    CreatedAt = s.CreatedAt,
                    CreatedByName = s.CreatedByNavigation != null ? s.CreatedByNavigation.UserName : "",
                    ApprovedAt = s.ApprovedAt,
                    ApprovedByName = s.ApprovedByNavigation != null ? s.ApprovedByNavigation.UserName : ""
                })
                .ToListAsync();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Status --" },
                new SelectListItem { Value = "Draft", Text = "Draft" },
                new SelectListItem { Value = "Submitted", Text = "Submitted" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Posted", Text = "Posted" },
                new SelectListItem { Value = "Rejected", Text = "Rejected" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = w.WarehouseName })
                .ToListAsync();
            warehouses.Insert(0, new SelectListItem { Value = "", Text = "-- All Warehouses --" });

            var model = new StockAdjustmentIndexViewModel
            {
                Adjustments = adjustments,
                SearchTerm = searchTerm ?? string.Empty,
                Status = status,
                WarehouseId = warehouseId,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Statuses = new SelectList(statuses, "Value", "Text", status),
                Warehouses = new SelectList(warehouses, "Value", "Text", warehouseId?.ToString())
            };

            return View(model);
        }

        // GET: StockAdjustments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(a => a.Warehouse)
                .Include(a => a.CreatedByNavigation)
                .Include(a => a.SubmittedByNavigation)
                .Include(a => a.ApprovedByNavigation)
                .Include(a => a.PostedByNavigation)
                .Include(a => a.StockAdjustmentLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
            {
                return NotFound();
            }

            var model = new StockAdjustmentViewModel
            {
                Id = adjustment.Id,
                AdjustmentNumber = adjustment.AdjustmentNumber,
                WarehouseId = adjustment.WarehouseId,
                WarehouseName = adjustment.Warehouse?.WarehouseName,
                AdjustmentDate = adjustment.AdjustmentDate.ToDateTime(TimeOnly.MinValue),
                AdjustmentType = adjustment.AdjustmentType,
                ReasonCategory = adjustment.ReasonCategory,
                ReasonDescription = adjustment.ReasonDescription,
                ReferenceNumber = adjustment.ReferenceNumber,
                Status = adjustment.Status,
                TotalValue = adjustment.TotalValue,
                Notes = adjustment.Notes,
                SubmittedAt = adjustment.SubmittedAt,
                SubmittedByName = adjustment.SubmittedByNavigation?.UserName,
                ApprovedAt = adjustment.ApprovedAt,
                ApprovedByName = adjustment.ApprovedByNavigation?.UserName,
                ApprovedComments = adjustment.ApprovedComments,
                PostedAt = adjustment.PostedAt,
                PostedByName = adjustment.PostedByNavigation?.UserName,
                Lines = adjustment.StockAdjustmentLines.Select(l => new StockAdjustmentLineViewModel
                {
                    Id = l.Id,
                    ArticleId = l.ArticleId,
                    ArticleCode = l.Article.ArticleCode,
                    ArticleName = l.Article.ArticleName,
                    SystemQuantity = l.SystemQuantity,
                    PhysicalQuantity = l.PhysicalQuantity,
                    Variance = l.Variance,
                    UnitCost = l.UnitCost,
                    TotalValue = l.TotalValue,
                    BatchNumber = l.BatchNumber,
                    SerialNumber = l.SerialNumber,
                    ExpiryDate = l.ExpiryDate.HasValue ? l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    Notes = l.Notes
                }).ToList()
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: StockAdjustments/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new StockAdjustmentViewModel
            {
                AdjustmentNumber = await GenerateAdjustmentNumber(),
                AdjustmentDate = DateTime.UtcNow,
                Status = "Draft",
                Lines = new List<StockAdjustmentLineViewModel> { new StockAdjustmentLineViewModel() }
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: StockAdjustments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockAdjustmentViewModel model)
        {
            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");
            ModelState.Remove("AdjustmentTypes");
            ModelState.Remove("ReasonCategories");
            ModelState.Remove("Lines");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.PhysicalQuantity != l.SystemQuantity).ToList() ?? new List<StockAdjustmentLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item with a variance.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // Calculate total value from valid lines
            model.TotalValue = validLines.Sum(l => (l.PhysicalQuantity - l.SystemQuantity) * l.UnitCost);

            if (ModelState.IsValid)
            {
                var adjustment = new StockAdjustment
                {
                    AdjustmentNumber = model.AdjustmentNumber,
                    WarehouseId = model.WarehouseId,
                    AdjustmentDate = DateOnly.FromDateTime(model.AdjustmentDate),
                    AdjustmentType = model.AdjustmentType,
                    ReasonCategory = model.ReasonCategory,
                    ReasonDescription = model.ReasonDescription,
                    ReferenceNumber = model.ReferenceNumber,
                    Status = "Draft",
                    Notes = model.Notes,
                    TotalValue = model.TotalValue,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.StockAdjustments.Add(adjustment);
                await _context.SaveChangesAsync();

                foreach (var line in validLines)
                {
                    var adjustmentLine = new StockAdjustmentLine
                    {
                        AdjustmentId = adjustment.Id,
                        ArticleId = line.ArticleId,
                        SystemQuantity = line.SystemQuantity,
                        PhysicalQuantity = line.PhysicalQuantity,
                        Variance = line.PhysicalQuantity - line.SystemQuantity,
                        UnitCost = line.UnitCost,
                        TotalValue = (line.PhysicalQuantity - line.SystemQuantity) * line.UnitCost,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                        Notes = line.Notes
                    };
                    _context.StockAdjustmentLines.Add(adjustmentLine);
                }

                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync("Create", "StockAdjustment", adjustment.Id, null,
                    JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, adjustment.TotalValue }),
                    $"Created stock adjustment: {adjustment.AdjustmentNumber}");

                TempData["SuccessMessage"] = $"Stock adjustment {adjustment.AdjustmentNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: StockAdjustments/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(a => a.StockAdjustmentLines)
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == "Draft");

            if (adjustment == null)
            {
                TempData["ErrorMessage"] = "Adjustment not found or cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            var model = new StockAdjustmentViewModel
            {
                Id = adjustment.Id,
                AdjustmentNumber = adjustment.AdjustmentNumber,
                WarehouseId = adjustment.WarehouseId,
                AdjustmentDate = adjustment.AdjustmentDate.ToDateTime(TimeOnly.MinValue),
                AdjustmentType = adjustment.AdjustmentType,
                ReasonCategory = adjustment.ReasonCategory,
                ReasonDescription = adjustment.ReasonDescription,
                ReferenceNumber = adjustment.ReferenceNumber,
                Status = adjustment.Status,
                TotalValue = adjustment.TotalValue,
                Notes = adjustment.Notes,
                Lines = adjustment.StockAdjustmentLines.Select(l => new StockAdjustmentLineViewModel
                {
                    Id = l.Id,
                    ArticleId = l.ArticleId,
                    SystemQuantity = l.SystemQuantity,
                    PhysicalQuantity = l.PhysicalQuantity,
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

        // POST: StockAdjustments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StockAdjustmentViewModel model)
        {
            if (id != model.Id) return NotFound();

            var adjustment = await _context.StockAdjustments
                .Include(a => a.StockAdjustmentLines)
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == "Draft");

            if (adjustment == null)
            {
                TempData["ErrorMessage"] = "Adjustment not found or cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");
            ModelState.Remove("AdjustmentTypes");
            ModelState.Remove("ReasonCategories");
            ModelState.Remove("Lines");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.PhysicalQuantity != l.SystemQuantity).ToList() ?? new List<StockAdjustmentLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item with a variance.");
                await PopulateDropdowns(model);
                return View(model);
            }

            // Calculate total value
            model.TotalValue = validLines.Sum(l => (l.PhysicalQuantity - l.SystemQuantity) * l.UnitCost);

            if (ModelState.IsValid)
            {
                // Update header
                adjustment.WarehouseId = model.WarehouseId;
                adjustment.AdjustmentDate = DateOnly.FromDateTime(model.AdjustmentDate);
                adjustment.AdjustmentType = model.AdjustmentType;
                adjustment.ReasonCategory = model.ReasonCategory;
                adjustment.ReasonDescription = model.ReasonDescription;
                adjustment.ReferenceNumber = model.ReferenceNumber;
                adjustment.Notes = model.Notes;
                adjustment.TotalValue = model.TotalValue;
                adjustment.UpdatedAt = DateTime.UtcNow;
                adjustment.UpdatedBy = GetCurrentUserId();

                // Remove old lines
                _context.StockAdjustmentLines.RemoveRange(adjustment.StockAdjustmentLines);

                // Add new lines
                foreach (var line in validLines)
                {
                    var adjustmentLine = new StockAdjustmentLine
                    {
                        AdjustmentId = adjustment.Id,
                        ArticleId = line.ArticleId,
                        SystemQuantity = line.SystemQuantity,
                        PhysicalQuantity = line.PhysicalQuantity,
                        Variance = line.PhysicalQuantity - line.SystemQuantity,
                        UnitCost = line.UnitCost,
                        TotalValue = (line.PhysicalQuantity - line.SystemQuantity) * line.UnitCost,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                        Notes = line.Notes
                    };
                    _context.StockAdjustmentLines.Add(adjustmentLine);
                }

                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync("Edit", "StockAdjustment", adjustment.Id,
                    JsonSerializer.Serialize(new { adjustment.AdjustmentNumber }),
                    JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, adjustment.TotalValue }),
                    $"Edited stock adjustment: {adjustment.AdjustmentNumber}");

                TempData["SuccessMessage"] = $"Stock adjustment {adjustment.AdjustmentNumber} updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: StockAdjustments/Submit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
                return Json(new { success = false, message = "Adjustment not found." });

            if (adjustment.Status != "Draft")
                return Json(new { success = false, message = "Only draft adjustments can be submitted." });

            adjustment.Status = "Submitted";
            adjustment.SubmittedAt = DateTime.UtcNow;
            adjustment.SubmittedBy = GetCurrentUserId();
            adjustment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("Submit", "StockAdjustment", adjustment.Id, null,
                JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, adjustment.TotalValue }),
                $"Submitted stock adjustment for approval: {adjustment.AdjustmentNumber}");

            return Json(new { success = true, message = $"Adjustment {adjustment.AdjustmentNumber} submitted for approval!" });
        }

        // POST: StockAdjustments/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments = null)
        {
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
                return Json(new { success = false, message = "Adjustment not found." });

            if (adjustment.Status != "Submitted")
                return Json(new { success = false, message = "Only submitted adjustments can be approved." });

            adjustment.Status = "Approved";
            adjustment.ApprovedAt = DateTime.UtcNow;
            adjustment.ApprovedBy = GetCurrentUserId();
            adjustment.ApprovedComments = comments;
            adjustment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("Approve", "StockAdjustment", adjustment.Id, null,
                JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, adjustment.TotalValue, comments }),
                $"Approved stock adjustment: {adjustment.AdjustmentNumber}");

            return Json(new { success = true, message = $"Adjustment {adjustment.AdjustmentNumber} approved!" });
        }

        // POST: StockAdjustments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason = null)
        {
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
                return Json(new { success = false, message = "Adjustment not found." });

            if (adjustment.Status != "Submitted")
                return Json(new { success = false, message = "Only submitted adjustments can be rejected." });

            adjustment.Status = "Rejected";
            adjustment.ApprovedComments = reason;
            adjustment.UpdatedAt = DateTime.UtcNow;
            adjustment.UpdatedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("Reject", "StockAdjustment", adjustment.Id, null,
                JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, reason }),
                $"Rejected stock adjustment: {adjustment.AdjustmentNumber}. Reason: {reason}");

            return Json(new { success = true, message = $"Adjustment {adjustment.AdjustmentNumber} rejected!" });
        }

        // POST: StockAdjustments/Post/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(int id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(a => a.StockAdjustmentLines)
                .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
                return Json(new { success = false, message = "Adjustment not found." });

            if (adjustment.Status != "Approved")
                return Json(new { success = false, message = "Only approved adjustments can be posted." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var line in adjustment.StockAdjustmentLines)
                {
                    // Update CurrentStock
                    var currentStock = await _context.CurrentStocks
                        .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                                   cs.WarehouseId == adjustment.WarehouseId &&
                                                   cs.BatchNumber == line.BatchNumber &&
                                                   cs.SerialNumber == line.SerialNumber);

                    var previousStock = currentStock?.Quantity ?? 0;
                    var newStock = previousStock + line.Variance;

                    if (currentStock != null)
                    {
                        currentStock.Quantity = newStock;
                        currentStock.LastUpdated = DateTime.UtcNow;
                    }
                    else if (line.Variance > 0)
                    {
                        currentStock = new CurrentStock
                        {
                            ArticleId = line.ArticleId,
                            WarehouseId = adjustment.WarehouseId,
                            Quantity = line.Variance,
                            ReservedQuantity = 0,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            ExpiryDate = line.ExpiryDate,
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.CurrentStocks.Add(currentStock);
                    }

                    // Create StockMovement record
                    var stockMovement = new StockMovement
                    {
                        MovementNumber = $"ADJ-{adjustment.AdjustmentNumber}",
                        ArticleId = line.ArticleId,
                        WarehouseId = adjustment.WarehouseId,
                        MovementType = "Adjustment",
                        ActivityType = adjustment.ReasonCategory,
                        Quantity = line.Variance,
                        PreviousStock = previousStock,
                        NewStock = newStock,
                        UnitCost = line.UnitCost,
                        TotalCost = line.TotalValue,
                        BatchNumber = line.BatchNumber,
                        SerialNumber = line.SerialNumber,
                        ExpiryDate = line.ExpiryDate,
                        MovementDate = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        ReferenceNumber = adjustment.AdjustmentNumber
                    };
                    _context.StockMovements.Add(stockMovement);
                }

                adjustment.Status = "Posted";
                adjustment.PostedAt = DateTime.UtcNow;
                adjustment.PostedBy = GetCurrentUserId();
                adjustment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _activityLogService.LogAsync("Post", "StockAdjustment", adjustment.Id, null,
                    JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, adjustment.TotalValue }),
                    $"Posted stock adjustment: {adjustment.AdjustmentNumber}");

                return Json(new { success = true, message = $"Adjustment {adjustment.AdjustmentNumber} posted successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Error posting adjustment: {ex.Message}" });
            }
        }

        // POST: StockAdjustments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason = null)
        {
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adjustment == null)
                return Json(new { success = false, message = "Adjustment not found." });

            if (adjustment.Status == "Posted")
                return Json(new { success = false, message = "Posted adjustments cannot be cancelled." });

            adjustment.Status = "Cancelled";
            adjustment.Notes = (adjustment.Notes != null ? adjustment.Notes + " | " : "") + $"Cancelled: {reason}";
            adjustment.UpdatedAt = DateTime.UtcNow;
            adjustment.UpdatedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("Cancel", "StockAdjustment", adjustment.Id, null,
                JsonSerializer.Serialize(new { adjustment.AdjustmentNumber, reason }),
                $"Cancelled stock adjustment: {adjustment.AdjustmentNumber}. Reason: {reason}");

            return Json(new { success = true, message = $"Adjustment {adjustment.AdjustmentNumber} cancelled!" });
        }

        // GET: StockAdjustments/GetProductStock
        [HttpGet]
        public async Task<IActionResult> GetProductStock(int articleId, int warehouseId)
        {
            var article = await _context.Articles.FindAsync(articleId);
            if (article == null)
                return Json(new { success = false });

            var currentStock = await _context.CurrentStocks
                .Where(cs => cs.ArticleId == articleId && cs.WarehouseId == warehouseId)
                .SumAsync(cs => cs.Quantity);

            return Json(new
            {
                success = true,
                articleCode = article.ArticleCode,
                articleName = article.ArticleName,
                systemQuantity = currentStock,
                unitCost = article.StandardCost,
                isBatchTracked = article.IsBatchTracked,
                isSerialized = article.IsSerialized,
                isExpiryTracked = article.IsExpiryTracked
            });
        }

        private async Task<string> GenerateAdjustmentNumber()
        {
            string prefix = "ADJ-";
            var lastAdjustment = await _context.StockAdjustments
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastAdjustment != null && lastAdjustment.AdjustmentNumber.StartsWith(prefix))
            {
                var match = Regex.Match(lastAdjustment.AdjustmentNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"{prefix}{nextNumber:D6}";
        }

        private async Task PopulateDropdowns(StockAdjustmentViewModel model)
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = w.WarehouseName })
                .ToListAsync();
            model.Warehouses = new SelectList(warehouses, "Value", "Text", model.WarehouseId);

            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsStockable)
                .OrderBy(a => a.ArticleName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.ArticleCode} - {a.ArticleName}" })
                .ToListAsync();
            model.Products = new SelectList(products, "Value", "Text");

            var adjustmentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "WriteOff", Text = "Write-Off (Stock Decrease)" },
                new SelectListItem { Value = "WriteOn", Text = "Write-On (Stock Increase)" },
                new SelectListItem { Value = "Correction", Text = "Correction (Error Fix)" }
            };
            model.AdjustmentTypes = new SelectList(adjustmentTypes, "Value", "Text", model.AdjustmentType);

            var reasonCategories = new List<SelectListItem>
            {
                new SelectListItem { Value = "Theft", Text = "Theft / Shrinkage" },
                new SelectListItem { Value = "Damage", Text = "Damaged Goods" },
                new SelectListItem { Value = "Expiry", Text = "Expired Products" },
                new SelectListItem { Value = "CountError", Text = "Counting Error" },
                new SelectListItem { Value = "Misplacement", Text = "Misplaced / Lost" },
                new SelectListItem { Value = "QualityIssue", Text = "Quality Issue" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
            model.ReasonCategories = new SelectList(reasonCategories, "Value", "Text", model.ReasonCategory);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}