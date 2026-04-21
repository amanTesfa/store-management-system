using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Extensions;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.RegularExpressions;
using Store_Management_System.Extensions;
namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class GoodsReceiptsController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public GoodsReceiptsController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: GoodsReceipts
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Supplier)
                .Include(v => v.TransactionReferenceSourceVouchers)
                .ThenInclude(r => r.TargetVoucher)
                .Where(v => v.VoucherType == "GRN")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Supplier != null && v.Supplier.Name.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var receipts = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new GoodsReceiptListViewModel
                {
                    Id = v.Id,
                    VoucherNumber = v.VoucherNumber,
                    PONumber = _context.TransactionReferences
                        .Where(r => r.TargetVoucherId == v.Id)
                        .Select(r => r.SourceVoucher.VoucherNumber)
                        .FirstOrDefault() ?? "N/A",
                    SupplierName = v.Supplier != null ? v.Supplier.Name : "N/A",
                    ReceiptDate = v.VoucherDate,
                    ItemCount = v.VoucherLines.Count,
                    Status = v.Status
                })
                .ToListAsync();

            var model = new GoodsReceiptIndexViewModel
            {
                GoodsReceipts = receipts,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }
        // GET: GoodsReceipts/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? poId = null)
        {
            var model = new GoodsReceiptViewModel
            {
                VoucherNumber = await GenerateVoucherNumber("GRN"),
                ReceiptDate = DateTime.UtcNow,
                Status = "Draft",
                Lines = new List<GoodsReceiptLineViewModel>()
            };

            await PopulatePurchaseOrders(model, poId);
            return View(model);
        }
        // GET: GoodsReceipts/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var grn = await _context.Vouchers
                .Include(v => v.Supplier)
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .Include(v => v.TransactionReferenceSourceVouchers)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "GRN");

            if (grn == null)
            {
                return NotFound();
            }

            // Get the associated PO number
            var poNumber = await _context.TransactionReferences
                .Where(r => r.TargetVoucherId == grn.Id)
                .Select(r => r.SourceVoucher.VoucherNumber)
                .FirstOrDefaultAsync();

            ViewBag.PONumber = poNumber ?? "N/A";

            return View(grn);
        }
        // POST: GoodsReceipts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoodsReceiptViewModel model)
        {
            ModelState.Remove("PurchaseOrders");

            // Validate at least one item to receive
            var itemsToReceive = model.Lines?.Where(l => l.QuantityToReceive > 0 && l.IsAccepted).ToList();
            if (itemsToReceive == null || !itemsToReceive.Any())
            {
                ModelState.AddModelError("", "Please select at least one item to receive.");
                await PopulatePurchaseOrders(model);
                return View(model);
            }

            // Validate no over-receiving
            foreach (var line in itemsToReceive)
            {
                if (line.QuantityToReceive > line.AvailableToReceive)
                {
                    ModelState.AddModelError("", $"Cannot receive more than ordered for {line.ArticleName}. Ordered: {line.AvailableToReceive + line.PreviouslyReceived}, Already received: {line.PreviouslyReceived}, Available: {line.AvailableToReceive}");
                }
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var errorMessage = string.Join(", ", errors);
                System.Diagnostics.Debug.WriteLine($"ModelState Invalid: {errorMessage}");

                await PopulatePurchaseOrders(model);
                return View(model);
            }
            if (ModelState.IsValid)
            {
                var po = await _context.Vouchers
                    .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                    .FirstOrDefaultAsync(v => v.Id == model.PurchaseOrderId && v.VoucherType == "PO");

                if (po == null)
                {
                    return Json(new { success = false, message = "Purchase Order not found." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create GRN voucher
                    var grn = new Voucher
                    {
                        VoucherNumber = model.VoucherNumber,
                        VoucherType = "GRN",
                        ActivityId = 2,
                        SupplierId = po.SupplierId,
                        VoucherDate = DateOnly.FromDateTime(model.ReceiptDate),
                        PostingDate = DateOnly.FromDateTime(model.ReceiptDate),
                        Status = "Posted",
                        Remarks = model.Remarks,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.Vouchers.Add(grn);
                    await _context.SaveChangesAsync();

                    // Create transaction reference linking GRN to PO
                    var transactionRef = new TransactionReference
                    {
                        SourceVoucherId = po.Id,
                        TargetVoucherId = grn.Id,
                        ReferenceType = "PO→GRN",
                        ReferenceDate = DateTime.UtcNow
                    };
                    _context.TransactionReferences.Add(transactionRef);

                    decimal totalLandedCost = 0;
                    var receivedLines = new List<(VoucherLine poLine, GoodsReceiptLineViewModel receiptLine)>();

                    foreach (var line in itemsToReceive)
                    {
                        var poLine = po.VoucherLines.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId);
                        if (poLine == null) continue;

                        // Calculate landed cost for this line
                        decimal lineLandedCost = 0;
                        decimal finalUnitCost = line.UnitPrice;

                        if (po.TotalLandedCost > 0 && po.LandedCostDistributionMethod != null)
                        {
                            // Will be distributed after all lines are processed
                            lineLandedCost = 0;
                            finalUnitCost = line.UnitPrice;
                        }

                        // Update received quantity on PO line
                        poLine.ReceivedQuantity = (poLine.ReceivedQuantity ?? 0) + line.QuantityToReceive;
                        var article = await _context.Articles.FindAsync(line.ArticleId);

                        // Create GRN line
                        var grnLine = new VoucherLine
                        {
                            VoucherId = grn.Id,
                            WarehouseId = po.WarehouseId ?? 1,
                            ArticleId = line.ArticleId,
                            Quantity = line.QuantityToReceive,
                            UnitPrice = line.UnitPrice,
                            UnitId = article?.BaseUnitId ?? 1,
                            LandedCostAmount = lineLandedCost,
                            FinalUnitCost = finalUnitCost,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                            LineTotal = line.QuantityToReceive * finalUnitCost,
                            IsAccepted = line.IsAccepted,
                            RejectionReason = line.RejectionReason
                        };
                        _context.VoucherLines.Add(grnLine);

                        receivedLines.Add((poLine, line));
                        totalLandedCost += lineLandedCost;

                        // Only update stock if accepted
                        if (line.IsAccepted)
                        {
                            await UpdateStock(po, line, grn.VoucherNumber);
                        }
                    }

                    // Distribute landed cost if any
                    if (po.TotalLandedCost > 0 && receivedLines.Any())
                    {
                        await DistributeLandedCost(po, receivedLines, po.TotalLandedCost ?? 0, grn.Id);
                    }

                    // Update PO status
                    var allLines = po.VoucherLines.ToList();
                    var totalOrdered = allLines.Sum(l => l.Quantity);
                    var totalReceived = allLines.Sum(l => l.ReceivedQuantity ?? 0);

                    if (totalReceived >= totalOrdered)
                    {
                        po.Status = "FullyReceived";
                        po.FullyReceivedAt = DateTime.UtcNow;
                    }
                    else if (totalReceived > 0)
                    {
                        po.Status = "PartiallyReceived";
                        po.PartiallyReceivedAt = DateTime.UtcNow;
                    }

                    grn.SubTotal = receivedLines.Sum(l => l.receiptLine.QuantityToReceive * l.receiptLine.UnitPrice);
                    grn.TotalAmount = grn.SubTotal + totalLandedCost;
                    var grnDto = grn.ToGoodsReceiptDto();
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    await _activityLogService.LogAsync("Create", "GoodsReceipt", grnDto.Id, null, grnDto.VoucherNumber, $"Created GRN {grnDto.VoucherNumber} linked to PO {po.VoucherNumber}");
                    TempData["SuccessMessage"] = $"Goods Receipt {grn.VoucherNumber} created and posted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            await PopulatePurchaseOrders(model);
            return View(model);
        }

        private async Task UpdateStock(Voucher po, GoodsReceiptLineViewModel line, string grnNumber)
        {
            var currentStock = await _context.CurrentStocks
                .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                           cs.WarehouseId == po.WarehouseId &&
                                           cs.BatchNumber == line.BatchNumber &&
                                           cs.SerialNumber == line.SerialNumber);

            if (currentStock != null)
            {
                currentStock.Quantity += line.QuantityToReceive;
                currentStock.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                currentStock = new CurrentStock
                {
                    ArticleId = line.ArticleId,
                    WarehouseId = po.WarehouseId ?? 1,
                    Quantity = line.QuantityToReceive,
                    ReservedQuantity = 0,
                    BatchNumber = line.BatchNumber,
                    SerialNumber = line.SerialNumber,
                    ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CurrentStocks.Add(currentStock);
            }

            // Create StockMovement
            var stockMovement = new StockMovement
            {
                MovementNumber = $"GRN-{grnNumber}",
                ArticleId = line.ArticleId,
                WarehouseId = po.WarehouseId ?? 1,
                MovementType = "In",
                ActivityType = "Goods Receipt",
                Quantity = line.QuantityToReceive,
                PreviousStock = (currentStock?.Quantity ?? 0) - line.QuantityToReceive,
                NewStock = currentStock?.Quantity ?? line.QuantityToReceive,
                UnitCost = line.FinalUnitCost,
                TotalCost = line.QuantityToReceive * line.FinalUnitCost,
                BatchNumber = line.BatchNumber,
                SerialNumber = line.SerialNumber,
                ExpiryDate = line.ExpiryDate.HasValue ? DateOnly.FromDateTime(line.ExpiryDate.Value) : null,
                MovementDate = DateTime.UtcNow,
                CreatedBy = GetCurrentUserId(),
                ReferenceNumber = po.VoucherNumber
            };
            _context.StockMovements.Add(stockMovement);
        }

        private async Task DistributeLandedCost(Voucher po, List<(VoucherLine poLine, GoodsReceiptLineViewModel receiptLine)> receivedLines, decimal totalLandedCost, int grnId)
        {
            decimal totalBaseValue = 0;
            var grnLines = await _context.VoucherLines.Where(l => l.VoucherId == grnId).ToListAsync();

            switch (po.LandedCostDistributionMethod)
            {
                case "ByValue":
                    totalBaseValue = receivedLines.Sum(l => l.receiptLine.QuantityToReceive * l.receiptLine.UnitPrice);
                    foreach (var line in receivedLines)
                    {
                        var lineValue = line.receiptLine.QuantityToReceive * line.receiptLine.UnitPrice;
                        var allocatedCost = totalBaseValue > 0 ? (lineValue / totalBaseValue) * totalLandedCost : 0;
                        var grnLine = grnLines.FirstOrDefault(l => l.ArticleId == line.receiptLine.ArticleId);
                        if (grnLine != null)
                        {
                            grnLine.LandedCostAmount = allocatedCost;
                            grnLine.FinalUnitCost = line.receiptLine.UnitPrice + (allocatedCost / line.receiptLine.QuantityToReceive);
                            grnLine.LineTotal = line.receiptLine.QuantityToReceive * (grnLine.FinalUnitCost ?? line.receiptLine.UnitPrice);
                        }
                    }
                    break;

                case "ByQuantity":
                    var totalQuantity = receivedLines.Sum(l => l.receiptLine.QuantityToReceive);
                    var costPerUnit = totalQuantity > 0 ? totalLandedCost / totalQuantity : 0;
                    foreach (var line in receivedLines)
                    {
                        var allocatedCost = line.receiptLine.QuantityToReceive * costPerUnit;
                        var grnLine = grnLines.FirstOrDefault(l => l.ArticleId == line.receiptLine.ArticleId);
                        if (grnLine != null)
                        {
                            grnLine.LandedCostAmount = allocatedCost;
                            grnLine.FinalUnitCost = line.receiptLine.UnitPrice + costPerUnit;
                            grnLine.LineTotal = line.receiptLine.QuantityToReceive * (grnLine.FinalUnitCost ?? line.receiptLine.UnitPrice);
                        }
                    }
                    break;
            }
        }

        // GET: GoodsReceipts/GetPOLines
        [HttpGet]
        public async Task<IActionResult> GetPOLines(int poId)
        {
            var po = await _context.Vouchers
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == poId && v.VoucherType == "PO");

            if (po == null)
                return Json(new { success = false });

            var lines = po.VoucherLines.Select(l => new
            {
                lineId = l.Id,
                articleId = l.ArticleId,
                articleCode = l.Article.ArticleCode,
                articleName = l.Article.ArticleName,
                orderedQuantity = l.Quantity,
                previouslyReceived = l.ReceivedQuantity ?? 0,
                availableToReceive = l.Quantity - (l.ReceivedQuantity ?? 0),
                unitPrice = l.UnitPrice,
                isBatchTracked = l.Article.IsBatchTracked,
                isSerialized = l.Article.IsSerialized,
                isExpiryTracked = l.Article.IsExpiryTracked
            }).ToList();

            return Json(new { success = true, lines = lines, poNumber = po.VoucherNumber, warehouseId = po.WarehouseId });
        }

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

        private async Task PopulatePurchaseOrders(GoodsReceiptViewModel model, int? selectedPoId = null)
        {
            var pos = await _context.Vouchers
                .Include(v => v.Supplier)
                .Where(v => v.VoucherType == "PO" && v.Status != "FullyReceived" && v.Status != "Cancelled")
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.VoucherNumber} - {v.Supplier.Name} - {v.VoucherDate}"
                })
                .ToListAsync();

            model.PurchaseOrders = new SelectList(pos, "Value", "Text", selectedPoId);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}