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
    public class ReturnsController : Controller
    {
        private readonly InventoryDbContext _context;

        public ReturnsController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: Returns/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .Include(v => v.Consignor)
                .Include(v => v.OriginalVoucher)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "RTV");

            if (voucher == null)
                return NotFound();

            var model = new ReturnToSupplierViewModel
            {
                Id = voucher.Id,
                ReturnNumber = voucher.VoucherNumber,
                OriginalReceiptId = voucher.OriginalVoucherId ?? 0,
                OriginalReceiptNumber = voucher.OriginalVoucher?.VoucherNumber,
                SupplierId = voucher.ConsignorId ?? 0,
                SupplierName = voucher.Consignor?.ConsignorName,
                WarehouseId = voucher.WarehouseId ?? 0,
                ReturnDate = voucher.VoucherDate.ToDateTime(TimeOnly.MinValue),
                ReturnReason = voucher.ReturnReason,
                Remarks = voucher.Remarks,
                Status = voucher.Status,
                Lines = voucher.VoucherLines.Select(l => new ReturnToSupplierLineViewModel
                {
                    ArticleId = l.ArticleId,
                    ArticleCode = l.Article?.ArticleCode,
                    ArticleName = l.Article?.ArticleName,
                    QuantityToReturn = l.Quantity,
                    UnitCost = l.UnitPrice,
                    Reason = l.Reason,
                    BatchNumber = l.BatchNumber,
                    SerialNumber = l.SerialNumber
                }).ToList()
            };

            return View(model);
        }

        // GET: Returns
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Consignor)
                .Include(v => v.OriginalVoucher)
                .Where(v => v.VoucherType == "RTV")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Consignor != null && v.Consignor.ConsignorName.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var list = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new ReturnListViewModel
                {
                    Id = v.Id,
                    VoucherNumber = v.VoucherNumber,
                    OriginalVoucherId = v.OriginalVoucherId,
                    OriginalVoucherNumber = v.OriginalVoucher != null ? v.OriginalVoucher.VoucherNumber : null,
                    SupplierName = v.Consignor != null ? v.Consignor.ConsignorName : string.Empty,
                    VoucherDate = v.VoucherDate,
                    ItemCount = v.VoucherLines.Count,
                    Status = v.Status
                })
                .ToListAsync();

            var model = new ReturnIndexViewModel
            {
                Returns = list,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: Returns/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? receiptId = null)
        {
            var model = new ReturnToSupplierViewModel
            {
                ReturnNumber = await GenerateReturnNumber(),
                ReturnDate = DateTime.UtcNow,
                Status = "Draft",
                Lines = new List<ReturnToSupplierLineViewModel>()
            };

            await PopulateReceipts(model, receiptId);
            return View(model);
        }

        // GET: Returns/GetReceiptLines
        [HttpGet]
        public async Task<IActionResult> GetReceiptLines(int receiptId)
        {
            var grn = await _context.Vouchers
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == receiptId && v.VoucherType == "GRN");

            if (grn == null)
                return Json(new { success = false });

            // Get already returned quantities
            var returns = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .Where(v => v.OriginalVoucherId == receiptId && v.IsReturn)
                .SelectMany(v => v.VoucherLines)
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(l => l.Quantity));

            var lines = grn.VoucherLines.Select(l => new
            {
                lineId = l.Id,
                articleId = l.ArticleId,
                articleCode = l.Article.ArticleCode,
                articleName = l.Article.ArticleName,
                receivedQuantity = l.Quantity,
                previouslyReturned = returns.ContainsKey(l.ArticleId) ? returns[l.ArticleId] : 0,
                availableToReturn = l.Quantity - (returns.ContainsKey(l.ArticleId) ? returns[l.ArticleId] : 0),
                unitCost = l.FinalUnitCost ?? l.UnitPrice,
                isBatchTracked = l.Article.IsBatchTracked,
                isSerialized = l.Article.IsSerialized,
                isExpiryTracked = l.Article.IsExpiryTracked,
                batchNumber = l.BatchNumber,
                serialNumber = l.SerialNumber,
                expiryDate = l.ExpiryDate
            }).ToList();

            return Json(new { success = true, lines = lines, supplierId = grn.ConsignorId });
        }

        // POST: Returns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReturnToSupplierViewModel model)
        {
            ModelState.Remove("Receipts");
            ModelState.Remove("Warehouses");

            var validLines = model.Lines?.Where(l => l.QuantityToReturn > 0).ToList() ?? new List<ReturnToSupplierLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to return.");
                await PopulateReceipts(model);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var originalGrn = await _context.Vouchers
                    .Include(v => v.VoucherLines)
                    .FirstOrDefaultAsync(v => v.Id == model.OriginalReceiptId);

                if (originalGrn == null)
                {
                    return Json(new { success = false, message = "Original receipt not found." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create return voucher
                    var returnVoucher = new Voucher
                    {
                        VoucherNumber = model.ReturnNumber,
                        VoucherType = "RTV", // Return to Vendor
                        ActivityId = 3, // Return activity
                        ConsignorId = originalGrn.ConsignorId,
                        VoucherDate = DateOnly.FromDateTime(model.ReturnDate),
                        PostingDate = DateOnly.FromDateTime(model.ReturnDate),
                        IsReturn = true,
                        OriginalVoucherId = originalGrn.Id,
                        ReturnReason = model.ReturnReason,
                        Remarks = model.Remarks,
                        Status = "Posted",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.Vouchers.Add(returnVoucher);
                    await _context.SaveChangesAsync();

                    foreach (var line in validLines)
                    {
                        // Create return line
                        var returnLine = new VoucherLine
                        {
                            VoucherId = returnVoucher.Id,
                            ArticleId = line.ArticleId,
                            Quantity = line.QuantityToReturn,
                            UnitPrice = line.UnitCost,
                            LineTotal = line.TotalValue,
                            Reason = line.Reason,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber
                        };
                        _context.VoucherLines.Add(returnLine);

                        // Update CurrentStock (decrease)
                        var currentStock = await _context.CurrentStocks
                            .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                                       cs.WarehouseId == model.WarehouseId &&
                                                       cs.BatchNumber == line.BatchNumber &&
                                                       cs.SerialNumber == line.SerialNumber);

                        if (currentStock != null)
                        {
                            currentStock.Quantity -= line.QuantityToReturn;
                            currentStock.LastUpdated = DateTime.UtcNow;
                        }

                        // Create StockMovement for return
                        var stockMovement = new StockMovement
                        {
                            MovementNumber = $"RTV-{returnVoucher.VoucherNumber}",
                            ArticleId = line.ArticleId,
                            WarehouseId = model.WarehouseId,
                            MovementType = "Out",
                            ActivityType = "Return to Supplier",
                            Quantity = -line.QuantityToReturn,
                            PreviousStock = currentStock?.Quantity ?? 0,
                            NewStock = (currentStock?.Quantity ?? 0) - line.QuantityToReturn,
                            UnitCost = line.UnitCost,
                            TotalCost = -line.TotalValue,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            MovementDate = DateTime.UtcNow,
                            CreatedBy = GetCurrentUserId(),
                            ReferenceNumber = originalGrn.VoucherNumber,
                            VoucherId = returnVoucher.Id
                        };
                        _context.StockMovements.Add(stockMovement);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Return {returnVoucher.VoucherNumber} created successfully!";
                    return RedirectToAction("Index", "GoodsReceipts");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            await PopulateReceipts(model);
            return View(model);
        }

        private async Task<string> GenerateReturnNumber()
        {
            var lastReturn = await _context.Vouchers
                .Where(v => v.VoucherNumber.StartsWith("RTV"))
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastReturn != null)
            {
                var match = Regex.Match(lastReturn.VoucherNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"RTV-{nextNumber:D6}";
        }

        private async Task PopulateReceipts(ReturnToSupplierViewModel model, int? selectedReceiptId = null)
        {
            var receipts = await _context.Vouchers
                .Include(v => v.Consignor)
                .Where(v => v.VoucherType == "GRN" && v.Status == "Posted")
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.VoucherNumber} - {v.Consignor.ConsignorName} - {v.VoucherDate}"
                })
                .ToListAsync();

            model.Receipts = new SelectList(receipts, "Value", "Text", selectedReceiptId);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}