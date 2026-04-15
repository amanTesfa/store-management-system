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
    public class GoodsReceiptsController : Controller
    {
        private readonly InventoryDbContext _context;

        public GoodsReceiptsController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: GoodsReceipts
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Consignor)
                .Where(v => v.VoucherType == "GRN")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Consignor != null && v.Consignor.ConsignorName.Contains(searchTerm)));
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
                    SupplierName = v.Consignor != null ? v.Consignor.ConsignorName : "",
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

        // POST: GoodsReceipts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoodsReceiptViewModel model)
        {
            ModelState.Remove("PurchaseOrders");

            if (ModelState.IsValid)
            {
                var po = await _context.Vouchers
                    .Include(v => v.VoucherLines)
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
                        ActivityId = 2, // Goods Receipt activity
                        ConsignorId = po.ConsignorId,
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
                        ReferenceType = "PO→GRN"
                    };
                    _context.TransactionReferences.Add(transactionRef);

                    decimal totalLandedCost = 0;
                    var receivedLines = new List<(VoucherLine poLine, GoodsReceiptLineViewModel receiptLine)>();

                    foreach (var line in model.Lines.Where(l => l.QuantityToReceive > 0 && l.IsAccepted))
                    {
                        var poLine = po.VoucherLines.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId);
                        if (poLine == null) continue;

                        // Update received quantity on PO line
                        poLine.ReceivedQuantity = (poLine.ReceivedQuantity ?? 0) + line.QuantityToReceive;
                        totalLandedCost += line.LandedCostAmount;

                        // Create GRN line
                        var grnLine = new VoucherLine
                        {
                            VoucherId = grn.Id,
                            ArticleId = line.ArticleId,
                            Quantity = line.QuantityToReceive,
                            UnitPrice = line.UnitPrice,
                            LandedCostAmount = line.LandedCostAmount,
                            FinalUnitCost = line.FinalUnitCost,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            ExpiryDate = line.ExpiryDate.HasValue ? line.ExpiryDate.Value: null,
                            LineTotal = line.QuantityToReceive * line.FinalUnitCost
                        };
                        _context.VoucherLines.Add(grnLine);
                        await _context.SaveChangesAsync();

                        receivedLines.Add((poLine, line));

                        // Update CurrentStock
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
                                ExpiryDate = line.ExpiryDate.HasValue ? line.ExpiryDate.Value : null,
                                LastUpdated = DateTime.UtcNow
                            };
                            _context.CurrentStocks.Add(currentStock);
                        }

                        // Create StockMovement
                        var stockMovement = new StockMovement
                        {
                            MovementNumber = $"GRN-{grn.VoucherNumber}",
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
                            ExpiryDate = line.ExpiryDate.HasValue ? line.ExpiryDate.Value : null,
                            MovementDate = DateTime.UtcNow,
                            CreatedBy = GetCurrentUserId(),
                            ReferenceNumber = po.VoucherNumber,
                            VoucherId = grn.Id
                        };
                        _context.StockMovements.Add(stockMovement);
                    }

                    // Update PO status based on received quantities
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

                    // Calculate and distribute landed cost if any
                    if (totalLandedCost > 0 && po.LandedCostDistributionMethod != null)
                    {
                        await DistributeLandedCost(po, receivedLines, totalLandedCost);
                    }

                    grn.SubTotal = receivedLines.Sum(l => l.receiptLine.QuantityToReceive * l.receiptLine.UnitPrice);
                    grn.TotalAmount = grn.SubTotal + totalLandedCost;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

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

        private async Task DistributeLandedCost(Voucher po, List<(VoucherLine poLine, GoodsReceiptLineViewModel receiptLine)> receivedLines, decimal totalLandedCost)
        {
            decimal totalBaseValue = 0;

            switch (po.LandedCostDistributionMethod)
            {
                case "ByValue":
                    totalBaseValue = receivedLines.Sum(l => l.receiptLine.QuantityToReceive * l.receiptLine.UnitPrice);
                    foreach (var line in receivedLines)
                    {
                        var lineValue = line.receiptLine.QuantityToReceive * line.receiptLine.UnitPrice;
                        var allocatedCost = totalBaseValue > 0 ? (lineValue / totalBaseValue) * totalLandedCost : 0;
                        line.receiptLine.LandedCostAmount = allocatedCost;
                        line.receiptLine.FinalUnitCost = line.receiptLine.UnitPrice + (allocatedCost / line.receiptLine.QuantityToReceive);
                    }
                    break;

                case "ByQuantity":
                    var totalQuantity = receivedLines.Sum(l => l.receiptLine.QuantityToReceive);
                    var costPerUnit = totalQuantity > 0 ? totalLandedCost / totalQuantity : 0;
                    foreach (var line in receivedLines)
                    {
                        var allocatedCost = line.receiptLine.QuantityToReceive * costPerUnit;
                        line.receiptLine.LandedCostAmount = allocatedCost;
                        line.receiptLine.FinalUnitCost = line.receiptLine.UnitPrice + costPerUnit;
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

            return Json(new { success = true, lines = lines, warehouseId = po.WarehouseId });
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
                .Include(v => v.Consignor)
                .Where(v => v.VoucherType == "PO" && v.Status != "FullyReceived" && v.Status != "Cancelled")
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.VoucherNumber} - {v.Consignor.ConsignorName} - {v.VoucherDate}"
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