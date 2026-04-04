using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize]
    public class ArticlesController : Controller
    {
        private readonly InventoryDbContext _context;

        public ArticlesController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: Articles
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int page = 1, int pageSize = 10)
        {
            var query = _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsActive)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.ArticleCode.Contains(searchTerm) ||
                                         a.ArticleName.Contains(searchTerm));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(a => a.ArticleCategory == categoryId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var articles = await query
                .OrderBy(a => a.ArticleName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticleListViewModel
                {
                    Id = a.Id,
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.ArticleName,
                    Description = a.Description,
                    CategoryName = a.Category != null ? a.Category.Name : null,
                    StandardPrice = a.StandardPrice,
                    CurrentStock = 0,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            var categories = await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
            categories.Insert(0, new SelectListItem { Value = "", Text = "-- All Categories --" });

            var model = new ArticleIndexViewModel
            {
                Articles = articles,
                SearchTerm = searchTerm ?? string.Empty,
                CategoryId = categoryId,
                Categories = new SelectList(categories, "Value", "Text", categoryId?.ToString()),
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: Articles/Create
        public async Task<IActionResult> Create()
        {
            var model = new ArticleCreateViewModel
            {
                ArticleCode = await GenerateArticleCode(),
                PrimaryBarcode = await GenerateArticleCode(),
                IsActive = true,
                IsStockable = true,
                IsPurchasable = true,
                IsSellable = true
            };

            await PopulateDropdowns();
            return View(model);
        }

        // POST: Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if ArticleCode already exists
                var existingArticle = await _context.Articles
                    .FirstOrDefaultAsync(a => a.ArticleCode == model.ArticleCode);

                if (existingArticle != null)
                {
                    ModelState.AddModelError("ArticleCode", "This Article Code already exists.");
                    await PopulateDropdowns(model.ArticleCategory, model.BaseUnitId);
                    return View(model);
                }

                // Create new article
                var article = new Article
                {
                    ArticleCode = model.ArticleCode,
                    ArticleName = model.ArticleName,
                    Description = model.Description,
                    ArticleCategory = model.ArticleCategory,  // FIXED: Using ArticleCategory
                    ArticleGroup = model.ArticleGroup,
                    BaseUnitId = model.BaseUnitId,
                    PurchaseUnitId = model.PurchaseUnitId,
                    SalesUnitId = model.SalesUnitId,
                    StandardCost = model.StandardCost,
                    StandardPrice = model.StandardPrice,
                    ReorderLevel = model.ReorderLevel,
                    MaxStockLevel = model.MaxStockLevel,
                    SafetyStock = model.SafetyStock,
                    IsActive = model.IsActive,
                    IsStockable = model.IsStockable,
                    IsPurchasable = model.IsPurchasable,
                    IsSellable = model.IsSellable,
                    IsSerialized = model.IsSerialized,
                    IsBatchTracked = model.IsBatchTracked,
                    IsExpiryTracked = model.IsExpiryTracked,
                    Weight = model.Weight,
                    Length = model.Length,
                    Width = model.Width,
                    Height = model.Height,
                    TaxRate = model.TaxRate,
                    TaxGroup = model.TaxGroup,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Articles.Add(article);
                await _context.SaveChangesAsync();

                // Create primary barcode
                var primaryBarcode = new ArticleBarcode
                {
                    ArticleId = article.Id,
                    Barcode = model.PrimaryBarcode,
                    BarcodeType = "CODE128",
                    IsPrimary = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ArticleBarcodes.Add(primaryBarcode);

                // Add additional barcodes if any
                if (model.AdditionalBarcodes != null)
                {
                    foreach (var barcodeValue in model.AdditionalBarcodes)
                    {
                        if (!string.IsNullOrWhiteSpace(barcodeValue))
                        {
                            var barcode = new ArticleBarcode
                            {
                                ArticleId = article.Id,
                                Barcode = barcodeValue,
                                BarcodeType = "EAN13",
                                IsPrimary = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ArticleBarcodes.Add(barcode);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Article '{article.ArticleName}' has been created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model.ArticleCategory, model.BaseUnitId);
            return View(model);
        }

        // GET: Articles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Barcodes)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            var primaryBarcode = article.Barcodes?.FirstOrDefault(b => b.IsPrimary);
            var additionalBarcodes = article.Barcodes?.Where(b => !b.IsPrimary).Select(b => b.Barcode).ToList();

            var model = new ArticleEditViewModel
            {
                Id = article.Id,
                ArticleCode = article.ArticleCode,
                ArticleName = article.ArticleName,
                Description = article.Description,
                ArticleCategory = article.ArticleCategory,  // FIXED: Using ArticleCategory
                ArticleGroup = article.ArticleGroup,
                BaseUnitId = article.BaseUnitId,
                PurchaseUnitId = article.PurchaseUnitId,
                SalesUnitId = article.SalesUnitId,
                StandardCost = article.StandardCost,
                StandardPrice = article.StandardPrice,
                ReorderLevel = article.ReorderLevel,
                MaxStockLevel = article.MaxStockLevel,
                SafetyStock = article.SafetyStock,
                IsActive = article.IsActive,
                IsStockable = article.IsStockable,
                IsPurchasable = article.IsPurchasable,
                IsSellable = article.IsSellable,
                IsSerialized = article.IsSerialized,
                IsBatchTracked = article.IsBatchTracked,
                IsExpiryTracked = article.IsExpiryTracked,
                Weight = article.Weight,
                Length = article.Length,
                Width = article.Width,
                Height = article.Height,
                TaxRate = article.TaxRate,
                TaxGroup = article.TaxGroup,
                PrimaryBarcode = primaryBarcode?.Barcode ?? article.ArticleCode,
                AdditionalBarcodes = additionalBarcodes ?? new List<string>()
            };

            await PopulateDropdowns(article.ArticleCategory, article.BaseUnitId);
            return View(model);
        }

        // POST: Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArticleEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var article = await _context.Articles
                    .Include(a => a.Barcodes)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    return NotFound();
                }

                // Update article properties
                article.ArticleName = model.ArticleName;
                article.Description = model.Description;
                article.ArticleCategory = model.ArticleCategory;  // FIXED: Using ArticleCategory
                article.ArticleGroup = model.ArticleGroup;
                article.BaseUnitId = model.BaseUnitId;
                article.PurchaseUnitId = model.PurchaseUnitId;
                article.SalesUnitId = model.SalesUnitId;
                article.StandardCost = model.StandardCost;
                article.StandardPrice = model.StandardPrice;
                article.ReorderLevel = model.ReorderLevel;
                article.MaxStockLevel = model.MaxStockLevel;
                article.SafetyStock = model.SafetyStock;
                article.IsActive = model.IsActive;
                article.IsStockable = model.IsStockable;
                article.IsPurchasable = model.IsPurchasable;
                article.IsSellable = model.IsSellable;
                article.IsSerialized = model.IsSerialized;
                article.IsBatchTracked = model.IsBatchTracked;
                article.IsExpiryTracked = model.IsExpiryTracked;
                article.Weight = model.Weight;
                article.Length = model.Length;
                article.Width = model.Width;
                article.Height = model.Height;
                article.TaxRate = model.TaxRate;
                article.TaxGroup = model.TaxGroup;
                article.UpdatedAt = DateTime.UtcNow;
                article.UpdatedBy = GetCurrentUserId();

                // Update barcodes
                // Update primary barcode
                var primaryBarcode = article.Barcodes?.FirstOrDefault(b => b.IsPrimary);
                if (primaryBarcode != null)
                {
                    primaryBarcode.Barcode = model.PrimaryBarcode;
                }

                // Remove old additional barcodes
                var oldAdditionalBarcodes = article.Barcodes?.Where(b => !b.IsPrimary).ToList();
                if (oldAdditionalBarcodes != null)
                {
                    _context.ArticleBarcodes.RemoveRange(oldAdditionalBarcodes);
                }

                // Add new additional barcodes
                if (model.AdditionalBarcodes != null)
                {
                    foreach (var barcodeValue in model.AdditionalBarcodes)
                    {
                        if (!string.IsNullOrWhiteSpace(barcodeValue))
                        {
                            var barcode = new ArticleBarcode
                            {
                                ArticleId = article.Id,
                                Barcode = barcodeValue,
                                BarcodeType = "EAN13",
                                IsPrimary = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ArticleBarcodes.Add(barcode);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Article '{article.ArticleName}' has been updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model.ArticleCategory, model.BaseUnitId);
            return View(model);
        }

        // GET: Articles/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.IsActive = false;
                article.UpdatedAt = DateTime.UtcNow;
                _context.Update(article);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Article '{article.ArticleName}' has been deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Articles/GetBarcodes/5
        [HttpGet]
        public async Task<IActionResult> GetBarcodes(int id)
        {
            var barcodes = await _context.ArticleBarcodes
                .Where(b => b.ArticleId == id && b.IsActive)
                .Select(b => new { b.Barcode, b.BarcodeType, b.IsPrimary })
                .ToListAsync();

            return Ok(barcodes);
        }

        // Helper: Generate Article Code
        private async Task<string> GenerateArticleCode()
        {
            // Format: Day + Month + Year (e.g., April 4, 2026 → "4426")
            string dayMonthYear = $"{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year % 100}";
            string prefix = $"ART-{dayMonthYear}-";

            var lastArticle = await _context.Articles
                .Where(a => a.ArticleCode.StartsWith(prefix))
                .OrderByDescending(a => a.ArticleCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastArticle != null)
            {
                var match = Regex.Match(lastArticle.ArticleCode, @"-(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        // Helper: Populate dropdowns
        private async Task PopulateDropdowns(int? selectedCategoryId = null, int? selectedBaseUnitId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            var units = await _context.UnitOfMeasures
                .Where(u => u.IsActive)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.UnitName })
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Value", "Text", selectedCategoryId);
            ViewBag.BaseUnits = new SelectList(units, "Value", "Text", selectedBaseUnitId);
            ViewBag.PurchaseUnits = new SelectList(units, "Value", "Text");
            ViewBag.SalesUnits = new SelectList(units, "Value", "Text");
        }

        // Helper: Get current user ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 1; // Default to admin
        }
    }
}