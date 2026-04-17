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
    public class InvoicesController : Controller
    {
        private readonly InventoryDbContext _context;

        public InvoicesController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Consignee)
                .Where(v => v.VoucherType == "INV")
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

            var invoices = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new InvoiceListViewModel
                {
                    Id = v.Id,
                    InvoiceNumber = v.VoucherNumber,
                    CustomerName = v.Consignee != null ? v.Consignee.ConsigneeName : "",
                    InvoiceDate = v.VoucherDate,
                    DueDate = v.DueDate,
                    TotalAmount = v.TotalAmount,
                    Status = v.Status,
                    StatusClass = v.Status == "Draft" ? "warning" :
                                  v.Status == "Posted" ? "info" :
                                  v.Status == "Paid" ? "success" : "danger"
                })
                .ToListAsync();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Status --" },
                new SelectListItem { Value = "Draft", Text = "Draft" },
                new SelectListItem { Value = "Posted", Text = "Posted" },
                new SelectListItem { Value = "Paid", Text = "Paid" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            var model = new InvoiceIndexViewModel
            {
                Invoices = invoices,
                SearchTerm = searchTerm ?? string.Empty,
                Status = status,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Statuses = new SelectList(statuses, "Value", "Text", status)
            };

            return View(model);
        }

        // GET: Invoices/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? soId = null)
        {
            var model = new InvoiceViewModel
            {
                InvoiceNumber = await GenerateInvoiceNumber(),
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Status = "Draft",
                Lines = new List<InvoiceLineViewModel>()
            };

            await PopulateSalesOrders(model, soId);
            return View(model);
        }

        // GET: Invoices/GetSalesOrderLines
        [HttpGet]
        public async Task<IActionResult> GetSalesOrderLines(int soId)
        {
            var so = await _context.Vouchers
                .Include(v => v.Consignee)
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == soId && v.VoucherType == "SO");

            if (so == null)
                return Json(new { success = false });

            // Get already invoiced quantities
            var invoiced = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .Where(v => v.OriginalVoucherId == soId && v.VoucherType == "INV")
                .SelectMany(v => v.VoucherLines)
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(l => l.Quantity));

            var lines = so.VoucherLines.Select(l => new
            {
                lineId = l.Id,
                articleId = l.ArticleId,
                articleCode = l.Article.ArticleCode,
                articleName = l.Article.ArticleName,
                orderedQuantity = l.Quantity,
                shippedQuantity = l.ShippedQuantity ?? 0,
                previouslyInvoiced = invoiced.ContainsKey(l.ArticleId) ? invoiced[l.ArticleId] : 0,
                availableToInvoice = (l.ShippedQuantity ?? 0) - (invoiced.ContainsKey(l.ArticleId) ? invoiced[l.ArticleId] : 0),
                unitPrice = l.UnitPrice,
                discountPercent = l.DiscountPercent
            }).ToList();

            return Json(new { success = true, lines = lines, soNumber = so.VoucherNumber, customerName = so.Consignee?.ConsigneeName });
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceViewModel model)
        {
            ModelState.Remove("SalesOrders");

            var validLines = model.Lines?.Where(l => l.Quantity > 0).ToList() ?? new List<InvoiceLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to invoice.");
                await PopulateSalesOrders(model);
                return View(model);
            }

            var so = await _context.Vouchers
                .Include(v => v.Consignee)
                .FirstOrDefaultAsync(v => v.Id == model.SalesOrderId && v.VoucherType == "SO");

            if (so == null)
            {
                return Json(new { success = false, message = "Sales Order not found." });
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create invoice voucher
                    var invoice = new Voucher
                    {
                        VoucherNumber = model.InvoiceNumber,
                        VoucherType = "INV",
                        ActivityId = 4, // Invoice activity
                        ConsigneeId = so.ConsigneeId,
                        VoucherDate = DateOnly.FromDateTime(model.InvoiceDate),
                        PostingDate = DateOnly.FromDateTime(model.InvoiceDate),
                        DueDate = model.DueDate.HasValue ? DateOnly.FromDateTime(model.DueDate.Value) : null,
                        SubTotal = model.SubTotal,
                        TaxAmount = model.TaxAmount,
                        DiscountAmount = model.DiscountAmount,
                        TotalAmount = model.TotalAmount,
                        Status = "Posted",
                        Remarks = model.Remarks,
                        OriginalVoucherId = so.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.Vouchers.Add(invoice);
                    await _context.SaveChangesAsync();
                    // Get the article to find its unit
                    var article = await _context.Articles.FindAsync(validLines.First().ArticleId);
                    // Add invoice lines
                    foreach (var line in validLines)
                    {
                        var invoiceLine = new VoucherLine
                        {
                            VoucherId = invoice.Id,
                            ArticleId = line.ArticleId,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            UnitId = article?.BaseUnitId ?? 1,
                            LineTotal = line.LineTotal,
                            OriginalVoucherLineId = line.SalesOrderLineId
                        };
                        _context.VoucherLines.Add(invoiceLine);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Invoice {invoice.VoucherNumber} created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            await PopulateSalesOrders(model);
            return View(model);
        }

        // GET: Invoices/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Vouchers
                .Include(v => v.Consignee)
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "INV");

            if (invoice == null)
                return NotFound();

            // Get associated SO number
            var soNumber = await _context.Vouchers
                .Where(v => v.Id == invoice.OriginalVoucherId)
                .Select(v => v.VoucherNumber)
                .FirstOrDefaultAsync();

            ViewBag.SONumber = soNumber ?? "N/A";

            return View(invoice);
        }

        // POST: Invoices/MarkAsPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, string? paymentReference = null)
        {
            var invoice = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "INV");

            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found." });

            if (invoice.Status != "Posted")
                return Json(new { success = false, message = "Only posted invoices can be marked as paid." });

            invoice.Status = "Paid";
            invoice.PaymentReference = paymentReference;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Update customer balance
            var customer = await _context.Consignees.FindAsync(invoice.ConsigneeId);
            if (customer != null)
            {
                customer.CurrentBalance -= invoice.TotalAmount;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Invoice {invoice.VoucherNumber} marked as paid!" });
        }
        // POST: Invoices/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason = null)
        {
            var invoice = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "INV");

            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found." });

            if (invoice.Status != "Draft" && invoice.Status != "Posted")
                return Json(new { success = false, message = "This invoice cannot be cancelled." });

            invoice.Status = "Cancelled";
            invoice.Remarks = (invoice.Remarks != null ? invoice.Remarks + " | " : "") + $"Cancelled: {reason}";
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Invoice {invoice.VoucherNumber} cancelled!" });
        }

        private async Task<string> GenerateInvoiceNumber()
        {
            var lastInvoice = await _context.Vouchers
                .Where(v => v.VoucherNumber.StartsWith("INV"))
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var match = Regex.Match(lastInvoice.VoucherNumber, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"INV-{nextNumber:D6}";
        }

        private async Task PopulateSalesOrders(InvoiceViewModel model, int? selectedSoId = null)
        {
            var salesOrders = await _context.Vouchers
                .Include(v => v.Consignee)
                .Where(v => v.VoucherType == "SO" && v.Status == "Shipped")
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.VoucherNumber} - {v.Consignee.ConsigneeName} - {v.VoucherDate}"
                })
                .ToListAsync();

            model.SalesOrders = new SelectList(salesOrders, "Value", "Text", selectedSoId);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}