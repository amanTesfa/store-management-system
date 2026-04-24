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
    public class CustomerReturnsController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;

        public CustomerReturnsController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: CustomerReturns
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Consignee)
                .Include(v => v.OriginalVoucher)
                .Where(v => v.VoucherType == "CRN") // Credit Return Note
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Consignee != null && v.Consignee.ConsigneeName.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var list = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new CustomerReturnListViewModel
                {
                    Id = v.Id,
                    VoucherNumber = v.VoucherNumber,
                    OriginalVoucherId = v.OriginalVoucherId,
                    OriginalInvoiceNumber = v.OriginalVoucher != null ? v.OriginalVoucher.VoucherNumber : null,
                    CustomerName = v.Consignee != null ? v.Consignee.ConsigneeName: string.Empty,
                    VoucherDate = v.VoucherDate,
                    ItemCount = v.VoucherLines.Count,
                    TotalAmount = v.VoucherLines.Sum(l => l.Quantity * l.UnitPrice),
                    Status = v.Status
                })
                .ToListAsync();

            var model = new CustomerReturnIndexViewModel
            {
                Returns = list,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: CustomerReturns/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .Include(v => v.Consignee)
                .Include(v => v.OriginalVoucher)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "CRN");

            if (voucher == null)
                return NotFound();

            var model = new CustomerReturnViewModel
            {
                Id = voucher.Id,
                ReturnNumber = voucher.VoucherNumber,
                OriginalInvoiceId = voucher.OriginalVoucherId ?? 0,
                OriginalInvoiceNumber = voucher.OriginalVoucher?.VoucherNumber,
                CustomerId = voucher.ConsigneeId ?? 0,
                CustomerName = voucher.Consignee?.ConsigneeName,
                WarehouseId = voucher.WarehouseId ?? 0,
                ReturnDate = voucher.VoucherDate.ToDateTime(TimeOnly.MinValue),
                ReturnReason = voucher.ReturnReason,
                Remarks = voucher.Remarks,
                Status = voucher.Status,
                Lines = voucher.VoucherLines.Select(l => new CustomerReturnLineViewModel
                {
                    ArticleId = l.ArticleId,
                    ArticleCode = l.Article?.ArticleCode,
                    ArticleName = l.Article?.ArticleName,
                    QuantityToReturn = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Reason = l.Reason,
                    BatchNumber = l.BatchNumber,
                    SerialNumber = l.SerialNumber
                }).ToList()
            };

            return View(model);
        }

        // GET: CustomerReturns/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? invoiceId = null)
        {
            var model = new CustomerReturnViewModel
            {
                ReturnNumber = await GenerateReturnNumber(),
                ReturnDate = DateTime.UtcNow,
                Status = "Draft",
                Lines = new List<CustomerReturnLineViewModel>()
            };

            await PopulateInvoices(model, invoiceId);
            await PopulateWarehouses(model);
            return View(model);
        }

        // GET: CustomerReturns/GetInvoiceLines
        [HttpGet]
        public async Task<IActionResult> GetInvoiceLines(int invoiceId)
        {
            var invoice = await _context.Vouchers
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == invoiceId && v.VoucherType == "INV");

            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found." });

            // Get already returned quantities for this invoice
            var returns = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .Where(v => v.OriginalVoucherId == invoiceId && v.IsReturn && v.VoucherType == "CRN")
                .SelectMany(v => v.VoucherLines)
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(l => l.Quantity));

            var lines = invoice.VoucherLines.Select(l => new
            {
                lineId = l.Id,
                articleId = l.ArticleId,
                articleCode = l.Article.ArticleCode,
                articleName = l.Article.ArticleName,
                soldQuantity = l.Quantity,
                previouslyReturned = returns.ContainsKey(l.ArticleId) ? returns[l.ArticleId] : 0,
                availableToReturn = l.Quantity - (returns.ContainsKey(l.ArticleId) ? returns[l.ArticleId] : 0),
                unitPrice = l.UnitPrice,
                isBatchTracked = l.Article.IsBatchTracked,
                isSerialized = l.Article.IsSerialized,
                batchNumber = l.BatchNumber,
                serialNumber = l.SerialNumber
            }).ToList();

            return Json(new
            {
                success = true,
                lines = lines,
                consigneeId  = invoice.ConsigneeId,
                invoiceNumber = invoice.VoucherNumber
            });
        }

        // POST: CustomerReturns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerReturnViewModel model)
        {
            ModelState.Remove("Invoices");
            ModelState.Remove("Warehouses");

            var validLines = model.Lines?
                .Where(l => l.QuantityToReturn > 0)
                .ToList() ?? new List<CustomerReturnLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to return.");
                await PopulateInvoices(model);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var originalInvoice = await _context.Vouchers
                    .Include(v => v.VoucherLines)
                    .FirstOrDefaultAsync(v => v.Id == model.OriginalInvoiceId && v.VoucherType == "INV");

                if (originalInvoice == null)
                {
                    return Json(new { success = false, message = "Original invoice not found." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the return voucher
                    var returnVoucher = new Voucher
                    {
                        VoucherNumber = model.ReturnNumber,
                        VoucherType = "CRN", // Credit Return Note (Customer Return)
                        ActivityId = 4, // Customer Return activity (adjust based on your Activity table)
                        ConsigneeId = originalInvoice.ConsigneeId,
                        VoucherDate = DateOnly.FromDateTime(model.ReturnDate),
                        PostingDate = DateOnly.FromDateTime(model.ReturnDate),
                        IsReturn = true,
                        OriginalVoucherId = originalInvoice.Id,
                        ReturnReason = model.ReturnReason,
                        Remarks = model.Remarks,
                        WarehouseId = model.WarehouseId,
                        Status = "Posted",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.Vouchers.Add(returnVoucher);
                    await _context.SaveChangesAsync();

                    foreach (var line in validLines)
                    {
                        var article = await _context.Articles.FindAsync(line.ArticleId);

                        // Create return line
                        var returnLine = new VoucherLine
                        {
                            VoucherId = returnVoucher.Id,
                            ArticleId = line.ArticleId,
                            Quantity = line.QuantityToReturn,
                            UnitPrice = line.UnitPrice,
                            UnitId = article?.BaseUnitId ?? 1,
                            LineTotal = line.TotalValue,
                            Reason = line.Reason,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber
                        };
                        _context.VoucherLines.Add(returnLine);

                        // Update CurrentStock (INCREASE stock for customer return)
                        var currentStock = await _context.CurrentStocks
                            .FirstOrDefaultAsync(cs => cs.ArticleId == line.ArticleId &&
                                                       cs.WarehouseId == model.WarehouseId &&
                                                       cs.BatchNumber == line.BatchNumber &&
                                                       cs.SerialNumber == line.SerialNumber);

                        if (currentStock != null)
                        {
                            currentStock.Quantity += line.QuantityToReturn; // INCREASE stock
                            currentStock.LastUpdated = DateTime.UtcNow;
                        }
                        else
                        {
                            // Create new stock entry if doesn't exist
                            _context.CurrentStocks.Add(new CurrentStock
                            {
                                ArticleId = line.ArticleId,
                                WarehouseId = model.WarehouseId,
                                Quantity = line.QuantityToReturn,
                                BatchNumber = line.BatchNumber,
                                SerialNumber = line.SerialNumber,
                                LastUpdated = DateTime.UtcNow
                            });
                        }

                        // Create StockMovement for customer return (INCOMING)
                        var stockMovement = new StockMovement
                        {
                            MovementNumber = $"CRN-{returnVoucher.VoucherNumber}",
                            ArticleId = line.ArticleId,
                            WarehouseId = model.WarehouseId,
                            MovementType = "In", // INCOMING for customer returns
                            ActivityType = "Customer Return",
                            Quantity = line.QuantityToReturn, // Positive for incoming
                            PreviousStock = currentStock?.Quantity ?? 0,
                            NewStock = (currentStock?.Quantity ?? 0) + line.QuantityToReturn,
                            UnitCost = line.UnitPrice,
                            TotalCost = line.TotalValue,
                            BatchNumber = line.BatchNumber,
                            SerialNumber = line.SerialNumber,
                            MovementDate = DateTime.UtcNow,
                            CreatedBy = GetCurrentUserId(),
                            ReferenceNumber = originalInvoice.VoucherNumber,
                            VoucherId = returnVoucher.Id
                        };
                        _context.StockMovements.Add(stockMovement);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Log activity
                    await _activityLogService.LogAsync(
                        "Create",
                        "CustomerReturn",
                        returnVoucher.Id,
                        null,
                        JsonSerializer.Serialize(new
                        {
                            returnVoucher.VoucherNumber,
                            returnVoucher.ConsigneeId,
                            returnVoucher.VoucherDate,
                            LineCount = validLines.Count,
                            TotalValue = validLines.Sum(l => l.TotalValue)
                        }),
                        $"Created customer return: {returnVoucher.VoucherNumber}");

                    TempData["SuccessMessage"] = $"Customer Return {returnVoucher.VoucherNumber} created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }

            await PopulateInvoices(model);
            await PopulateWarehouses(model);
            return View(model);
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private async Task<string> GenerateReturnNumber()
        {
            var lastReturn = await _context.Vouchers
                .Where(v => v.VoucherNumber.StartsWith("CRN"))
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastReturn != null)
            {
                var match = Regex.Match(lastReturn.VoucherNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"CRN-{nextNumber:D6}";
        }

        private async Task PopulateInvoices(CustomerReturnViewModel model, int? selectedInvoiceId = null)
        {
            // Allow invoices that are Posted OR Partially returned
            var allowedStatuses = new[] { "Posted", "Partially Returned" };

            var invoices = await _context.Vouchers
                .Include(v => v.Consignee)
                .Where(v => v.VoucherType == "INV" && allowedStatuses.Contains(v.Status))
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.VoucherNumber} - {v.Consignee.ConsigneeName} - {v.VoucherDate} [{v.Status}]"
                })
                .ToListAsync();

            model.Invoices = new SelectList(invoices, "Value", "Text", selectedInvoiceId);
        }

        private async Task PopulateWarehouses(CustomerReturnViewModel model)
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem
                {
                    Value = w.Id.ToString(),
                    Text = $"{w.WarehouseCode} - {w.WarehouseName}"
                })
                .ToListAsync();

            model.Warehouses = new SelectList(warehouses, "Value", "Text");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}