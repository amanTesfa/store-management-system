using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Extensions;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class PurchaseOrdersController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public PurchaseOrdersController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: PurchaseOrders
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.Supplier)
                .Where(v => v.VoucherType == "PO" && !v.IsReturn)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VoucherNumber.Contains(searchTerm) ||
                                         (v.Supplier != null && v.Supplier.Name.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var purchaseOrders = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new PurchaseOrderListViewModel
                {
                    Id = v.Id,
                    VoucherNumber = v.VoucherNumber,
                    SupplierName = v.Supplier != null ? v.Supplier.Name : "",
                    VoucherDate = v.VoucherDate,
                    ExpectedDate = v.ExpectedDate,
                    TotalAmount = v.TotalAmount,
                    Status = v.Status,
                    StatusClass = v.Status == "Draft" ? "warning" :
                                  v.Status == "Approved" ? "info" :
                                  v.Status == "Sent" ? "primary" :
                                  v.Status == "PartiallyReceived" ? "secondary" :
                                  v.Status == "FullyReceived" ? "success" : "danger",
                    ItemCount = v.VoucherLines.Count,
                    ReceivedCount = ((int)v.VoucherLines.Sum(l => l.ReceivedQuantity ?? 0))
                })
                .ToListAsync();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Status --" },
                new SelectListItem { Value = "Draft", Text = "Draft" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Sent", Text = "Sent to Supplier" },
                new SelectListItem { Value = "PartiallyReceived", Text = "Partially Received" },
                new SelectListItem { Value = "FullyReceived", Text = "Fully Received" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            var model = new PurchaseOrderIndexViewModel
            {
                PurchaseOrders = purchaseOrders,
                SearchTerm = searchTerm ?? string.Empty,
                Status = status,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Statuses = new SelectList(statuses, "Value", "Text", status)
            };

            return View(model);
        }

        // GET: PurchaseOrders/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PurchaseOrderViewModel
            {
                VoucherNumber = await GenerateVoucherNumber("PO"),
                Status = "Draft",
                VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExpectedDate = DateTime.UtcNow.AddDays(14),
                Lines = new List<PurchaseOrderLineViewModel> { new PurchaseOrderLineViewModel() }
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel model)
        {
            ModelState.Remove("Suppliers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");
            ModelState.Remove("DistributionMethods");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<PurchaseOrderLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item.");
                await PopulateDropdowns(model);
                return View(model);
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
                    VoucherType = "PO",
                    ActivityId = 1, // Purchase activity ID
                    SupplierId = model.SupplierId,
                    VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PostingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpectedDate = model.ExpectedDate.HasValue ? DateOnly.FromDateTime(model.ExpectedDate.Value) : null,
                    SubTotal = model.SubTotal,
                    DiscountAmount = 0,
                    TaxAmount = 0,
                    TotalAmount = model.TotalAmount,
                    Status = model.Status,
                    Remarks = model.Remarks,
                    ShippingCost = model.ShippingCost,
                    HandlingCost = model.HandlingCost,
                    InsuranceCost = model.InsuranceCost,
                    OtherCost = model.OtherCost,
                    TotalLandedCost = model.TotalCharges,
                    LandedCostDistributionMethod = model.LandedCostDistributionMethod,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                // Add lines
                foreach (var line in validLines)
                {
                    var article = await _context.Articles.FindAsync(line.ArticleId);

                    var voucherLine = new VoucherLine
                    {
                        VoucherId = voucher.Id,
                        ArticleId = line.ArticleId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        DiscountPercent = line.DiscountPercent,
                        LineTotal = line.LineTotal,
                        Description = line.Description,
                         UnitId = article?.BaseUnitId ?? 1,
                          WarehouseId = model.WarehouseId
                    };
                    _context.VoucherLines.Add(voucherLine);
                }
                var poDto = voucher.ToPurchaseOrderDto();
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Create", "PurchaseOrder", poDto.Id, newValue: $"Created PO {poDto.VoucherNumber} with {validLines.Count} lines.");
                TempData["SuccessMessage"] = $"Purchase Order {voucher.VoucherNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: PurchaseOrders/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null || voucher.Status != "Draft")
            {
                TempData["ErrorMessage"] = voucher == null ? "Purchase Order not found." : "Only draft orders can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var model = new PurchaseOrderViewModel
            {
                Id = voucher.Id,
                VoucherNumber = voucher.VoucherNumber,
                SupplierId = voucher.SupplierId ?? 0,
                WarehouseId = voucher.WarehouseId ?? 0,
                ExpectedDate = voucher.ExpectedDate.HasValue ? voucher.ExpectedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                Remarks = voucher.Remarks,
                Status = voucher.Status,
                ShippingCost = voucher.ShippingCost,
                HandlingCost = voucher.HandlingCost,
                InsuranceCost = voucher.InsuranceCost,
                OtherCost = voucher.OtherCost,
                LandedCostDistributionMethod = voucher.LandedCostDistributionMethod,
                Lines = voucher.VoucherLines.Select(l => new PurchaseOrderLineViewModel
                {
                    Id = l.Id,
                    ArticleId = l.ArticleId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent,
                    Description = l.Description
                }).ToList()
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: PurchaseOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderViewModel model)
        {
            if (id != model.Id) return NotFound();

            var voucher = await _context.Vouchers
                .Include(v => v.VoucherLines)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null || voucher.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Cannot edit non-draft order.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Suppliers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Products");
            ModelState.Remove("DistributionMethods");

            var validLines = model.Lines?.Where(l => l.ArticleId > 0 && l.Quantity > 0).ToList() ?? new List<PurchaseOrderLineViewModel>();

            if (!validLines.Any())
            {
                ModelState.AddModelError("", "Please add at least one valid line item.");
                await PopulateDropdowns(model);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Update header
                voucher.SupplierId = model.SupplierId;
                voucher.WarehouseId = model.WarehouseId;
                voucher.ExpectedDate = model.ExpectedDate.HasValue ? DateOnly.FromDateTime(model.ExpectedDate.Value) : null;
                voucher.Remarks = model.Remarks;
                voucher.SubTotal = model.SubTotal;
                voucher.TotalAmount = model.TotalAmount;
                voucher.ShippingCost = model.ShippingCost;
                voucher.HandlingCost = model.HandlingCost;
                voucher.InsuranceCost = model.InsuranceCost;
                voucher.OtherCost = model.OtherCost;
                voucher.TotalLandedCost = model.TotalCharges;
                voucher.LandedCostDistributionMethod = model.LandedCostDistributionMethod;
                voucher.UpdatedAt = DateTime.UtcNow;
                voucher.UpdatedBy = GetCurrentUserId();

                // Remove old lines
                _context.VoucherLines.RemoveRange(voucher.VoucherLines);

                // Add new lines
                foreach (var line in validLines)
                {
                    var article = await _context.Articles.FindAsync(line.ArticleId);

                    var voucherLine = new VoucherLine
                    {
                        VoucherId = voucher.Id,
                        ArticleId = line.ArticleId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        DiscountPercent = line.DiscountPercent,
                        LineTotal = line.LineTotal,
                        Description = line.Description,
                        UnitId = article?.BaseUnitId ?? 1,
                        WarehouseId = model.WarehouseId
                    };
                    _context.VoucherLines.Add(voucherLine);
                }
                var poDto = voucher.ToPurchaseOrderDto();
                await _context.SaveChangesAsync();
                   await _activityLogService.LogAsync("Edit", "PurchaseOrder", poDto.Id, oldValue: $"Edited PO {poDto.VoucherNumber}", newValue: $"Updated PO {poDto.VoucherNumber} with {validLines.Count} lines.");
                TempData["SuccessMessage"] = $"Purchase Order {voucher.VoucherNumber} updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: PurchaseOrders/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments = null)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null)
                return Json(new { success = false, message = "Purchase Order not found." });

            if (voucher.Status != "Draft")
                return Json(new { success = false, message = "Only draft orders can be approved." });

            voucher.Status = "Approved";
            voucher.ApprovedAt = DateTime.UtcNow;
            voucher.ApprovedBy = GetCurrentUserId();
            voucher.ApprovalComments = comments;
            voucher.UpdatedAt = DateTime.UtcNow;
            var poDto = voucher.ToPurchaseOrderDto();
            await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Approve", "PurchaseOrder", poDto.Id, oldValue: $"Approved PO {poDto.VoucherNumber}", newValue: $"PO {poDto.VoucherNumber} approved with comments: {comments}");
            return Json(new { success = true, message = $"Purchase Order {voucher.VoucherNumber} approved successfully!" });
        }

        // POST: PurchaseOrders/SendToSupplier/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToSupplier(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null)
                return Json(new { success = false, message = "Purchase Order not found." });

            if (voucher.Status != "Approved")
                return Json(new { success = false, message = "Only approved orders can be sent to supplier." });

            voucher.Status = "Sent";
            voucher.SentToSupplierAt = DateTime.UtcNow;
            voucher.UpdatedAt = DateTime.UtcNow;
            var poDto = voucher.ToPurchaseOrderDto();
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("SendToSupplier", "PurchaseOrder", poDto.Id, oldValue: $"Sent PO {poDto.VoucherNumber} to supplier", newValue: $"PO {poDto.VoucherNumber} sent to supplier at {voucher.SentToSupplierAt}");
            return Json(new { success = true, message = $"Purchase Order {voucher.VoucherNumber} sent to supplier!" });
        }

        // POST: PurchaseOrders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason = null)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null)
                return Json(new { success = false, message = "Purchase Order not found." });

            if (voucher.Status != "Draft" && voucher.Status != "Approved" && voucher.Status != "Sent")
                return Json(new { success = false, message = "This order cannot be cancelled." });

            voucher.Status = "Cancelled";
            voucher.Remarks = (voucher.Remarks != null ? voucher.Remarks + " | " : "") + $"Cancelled: {reason}";
            voucher.UpdatedAt = DateTime.UtcNow;
            var poDto = voucher.ToPurchaseOrderDto();
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("Cancel", "PurchaseOrder", poDto.Id, oldValue: $"Cancelled PO {poDto.VoucherNumber}", newValue: $"PO {poDto.VoucherNumber} cancelled with reason: {reason}");
            return Json(new { success = true, message = $"Purchase Order {voucher.VoucherNumber} cancelled!" });
        }

        // GET: PurchaseOrders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Supplier)
                .Include(v => v.VoucherLines)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(v => v.Id == id && v.VoucherType == "PO");

            if (voucher == null)
                return NotFound();

            return View(voucher);
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

        private async Task PopulateDropdowns(PurchaseOrderViewModel model)
        {
            var suppliers = await _context.Suppliers
       .Where(s => s.IsActive && !s.IsDeleted)
       .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
       .ToListAsync();

            model.Suppliers = new SelectList(suppliers, "Value", "Text", model.SupplierId);

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = $"{w.WarehouseCode} - {w.WarehouseName}" })
                .ToListAsync();
            model.Warehouses = new SelectList(warehouses, "Value", "Text", model.WarehouseId);

            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsPurchasable)
                .OrderBy(a => a.ArticleName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.ArticleCode} - {a.ArticleName}" })
                .ToListAsync();
            model.Products = new SelectList(products, "Value", "Text");

            var methods = new List<SelectListItem>
            {
                new SelectListItem { Value = "ByValue", Text = "By Value (Proportional to product cost)" },
                new SelectListItem { Value = "ByQuantity", Text = "By Quantity (Equal per unit)" },
                new SelectListItem { Value = "ByWeight", Text = "By Weight (if weight is specified)" }
            };
            model.DistributionMethods = new SelectList(methods, "Value", "Text", model.LandedCostDistributionMethod);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}