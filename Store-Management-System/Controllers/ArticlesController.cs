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
    [Authorize]
    public class ArticlesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public ArticlesController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: Articles
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int page = 1, int pageSize = 10)
        {
            var query = _context.Articles
                .Include(a => a.ArticleCategoryNavigation)
                .Where(a => a.IsActive)
                .AsQueryable();
            // Get stock levels from CurrentStock table
            var stockLevels = await _context.CurrentStocks
                .GroupBy(cs => cs.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalStock = g.Sum(cs => cs.Quantity) })
                .ToDictionaryAsync(k => k.ArticleId, v => v.TotalStock);

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
                .OrderBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticleListViewModel
                {
                    Id = a.Id,
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.ArticleName,
                    Description = a.Description,
                    CategoryName = a.ArticleCategoryNavigation != null ? a.ArticleCategoryNavigation.Name : null,
                    StandardPrice = a.StandardPrice,
                    CurrentStock = stockLevels.ContainsKey(a.Id) ? (int)stockLevels[a.Id] : 0, 
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
            var model = new Store_Management_System.ViewModels.ArticleCreateViewModel
            {
                ArticleCode = await GenerateArticleCode(),
                PrimaryBarcode = await GenerateArticleCode(),
                BaseUnitId = 1,
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
        public async Task<IActionResult> Create(Store_Management_System.ViewModels.ArticleCreateViewModel model)
        {
            ModelState.Remove("Categories");
            ModelState.Remove("BaseUnits");
            ModelState.Remove("PurchaseUnits");
            ModelState.Remove("SalesUnits");


            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error}");
                }
                TempData["failureMessage"] = $"this errors '{errors}' has been found";
                return RedirectToAction(nameof(Index));
            }

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
                    ArticleCategory = model.ArticleCategory,  
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
                var articleDto = article.ToDto();
                await _activityLogService.LogAsync("Create", "Article", article.Id, null,
                    
    JsonSerializer.Serialize(articleDto), $"Created article: {article.ArticleName}");

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
                .Include(a => a.ArticleBarcodes)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            var primaryBarcode = article.ArticleBarcodes?.FirstOrDefault(b => b.IsPrimary);
            var additionalBarcodes = article.ArticleBarcodes?.Where(b => !b.IsPrimary).Select(b => b.Barcode).ToList();

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
            ModelState.Remove("Categories");
            ModelState.Remove("BaseUnits");
            ModelState.Remove("PurchaseUnits");
            ModelState.Remove("SalesUnits");
            if (id != model.Id)
            {
                return NotFound();
            }
          
            if (ModelState.IsValid)
            {
                var article = await _context.Articles
                    .Include(a => a.ArticleBarcodes)
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
                var primaryBarcode = article.ArticleBarcodes?.FirstOrDefault(b => b.IsPrimary);
                if (primaryBarcode != null)
                {
                    primaryBarcode.Barcode = model.PrimaryBarcode;
                }

                // Remove old additional barcodes
                var oldAdditionalBarcodes = article.ArticleBarcodes?.Where(b => !b.IsPrimary).ToList();
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
                var articleDto = article.ToDto();       
                await _activityLogService.LogAsync("Edit", "Article", article.Id,null,
    JsonSerializer.Serialize(articleDto), 
    $"Updated article: {article.ArticleName}");
                TempData["SuccessMessage"] = $"Article '{article.ArticleName}' has been updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model.ArticleCategory, model.BaseUnitId);
            return View(model);
        }
        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var article = await _context.Articles
                .Include(a => a.ArticleCategoryNavigation)
                .Include(a => a.ArticleBarcodes)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            // Get current stock from CurrentStock table
            var currentStock = await _context.CurrentStocks
                .Where(cs => cs.ArticleId == id)
                .SumAsync(cs => cs.Quantity);

            // Get recent stock movements
            var recentMovements = await _context.StockMovements
                .Include(s => s.Warehouse)
                .Where(s => s.ArticleId == id)
                .OrderByDescending(s => s.MovementDate)
                .Take(10)
                .Select(s => new RecentStockMovementViewModel
                {
                    MovementDate = s.MovementDate,
                    MovementType = s.MovementType,
                    Quantity = (int)s.Quantity,
                    UnitCost = s.UnitCost,
                    Reference = s.ReferenceNumber ?? s.MovementNumber,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.WarehouseName : "N/A"
                })
                .ToListAsync();

            var primaryBarcode = article.ArticleBarcodes?.FirstOrDefault(b => b.IsPrimary);
            var additionalBarcodes = article.ArticleBarcodes?.Where(b => !b.IsPrimary).ToList();

            // Add current stock to ViewBag or create a ViewModel
            ViewBag.CurrentStock = (int)currentStock;
            ViewBag.RecentMovements = recentMovements;
            ViewBag.PrimaryBarcode = primaryBarcode;
            ViewBag.AdditionalBarcodes = additionalBarcodes;
            ViewBag.ReorderLevel = article.ReorderLevel;
            ViewBag.MaxStockLevel = article.MaxStockLevel;
            ViewBag.SafetyStock = article.SafetyStock;

            return View(article);
        }
        // GET: Articles/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.Articles
             .Include(a => a.ArticleCategoryNavigation)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var article = await _context.Articles.FindAsync(id);
                if (article != null)
                {
                    article.IsActive = false;
                    article.UpdatedAt = DateTime.UtcNow;
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                    await _activityLogService.LogAsync("Delete", "Article", id, null, null,
    $"Deleted article: {article.ArticleName}");
                    return Json(new { success = true, message = $"Article '{article.ArticleName}' has been deleted successfully!" });
                }

                return Json(new { success = false, message = "Article not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // GET: Articles/Import
        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        // POST: Articles/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Import));
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
            {
                TempData["ErrorMessage"] = "Please upload a CSV or Excel (.xlsx/.xls) file.";
                return RedirectToAction(nameof(Import));
            }

            try
            {
                List<ArticleImportDto> records;

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
                    return RedirectToAction(nameof(Import));
                }

                // Validate and import
                var result = await ImportRecords(records);

                // Store result in TempData for display
                TempData["ImportResult"] = JsonSerializer.Serialize(result);

                if (result.SuccessCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {result.SuccessCount} articles!";
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
                return RedirectToAction(nameof(Import));
            }
        }

        // POST: Articles/PreviewImport
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
                List<ArticleImportDto> records;

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
                    r.ArticleName,
                    r.ArticleCode,
                    r.CategoryName,
                    r.StandardPrice,
                    r.StandardCost,
                    r.BaseUnit,
                    r.Description,
                    IsValid = !string.IsNullOrWhiteSpace(r.ArticleName) && !string.IsNullOrWhiteSpace(r.CategoryName)
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

        // Helper: Parse CSV
        private async Task<List<ArticleImportDto>> ParseCsvFile(IFormFile file)
        {
            var records = new List<ArticleImportDto>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(headerLine))
                return records;

            var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"').ToLower()).ToArray();

            // Map column indices
            var nameIndex = Array.FindIndex(headers, h => h.Contains("name") && !h.Contains("category"));
            var codeIndex = Array.FindIndex(headers, h => h.Contains("code"));
            var categoryIndex = Array.FindIndex(headers, h => h.Contains("category"));
            var priceIndex = Array.FindIndex(headers, h => h.Contains("price") && !h.Contains("cost"));
            var costIndex = Array.FindIndex(headers, h => h.Contains("cost"));
            var unitIndex = Array.FindIndex(headers, h => h.Contains("unit") && !h.Contains("price") && !h.Contains("cost"));
            var descIndex = Array.FindIndex(headers, h => h.Contains("desc"));
            var groupIndex = Array.FindIndex(headers, h => h.Contains("group"));
            var reorderIndex = Array.FindIndex(headers, h => h.Contains("reorder"));
            var safetyIndex = Array.FindIndex(headers, h => h.Contains("safety"));
            var taxRateIndex = Array.FindIndex(headers, h => h.Contains("tax"));
            var barcodeIndex = Array.FindIndex(headers, h => h.Contains("barcode"));

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = ParseCsvLine(line);

                var record = new ArticleImportDto
                {
                    ArticleName = nameIndex >= 0 && nameIndex < values.Count ? values[nameIndex].Trim() : "",
                    ArticleCode = codeIndex >= 0 && codeIndex < values.Count ? values[codeIndex].Trim() : "",
                    CategoryName = categoryIndex >= 0 && categoryIndex < values.Count ? values[categoryIndex].Trim() : "",
                    StandardPrice = priceIndex >= 0 && priceIndex < values.Count ? ParseDecimal(values[priceIndex]) : 0,
                    StandardCost = costIndex >= 0 && costIndex < values.Count ? ParseDecimal(values[costIndex]) : 0,
                    BaseUnit = unitIndex >= 0 && unitIndex < values.Count ? values[unitIndex].Trim() : "Piece",
                    Description = descIndex >= 0 && descIndex < values.Count ? values[descIndex].Trim() : "",
                    ArticleGroup = groupIndex >= 0 && groupIndex < values.Count ? values[groupIndex].Trim() : "",
                    ReorderLevel = reorderIndex >= 0 && reorderIndex < values.Count ? ParseDecimal(values[reorderIndex]) : 0,
                    SafetyStock = safetyIndex >= 0 && safetyIndex < values.Count ? ParseDecimal(values[safetyIndex]) : 0,
                    TaxRate = taxRateIndex >= 0 && taxRateIndex < values.Count ? ParseDecimal(values[taxRateIndex]) : 0,
                    PrimaryBarcode = barcodeIndex >= 0 && barcodeIndex < values.Count ? values[barcodeIndex].Trim() : ""
                };

                if (!string.IsNullOrWhiteSpace(record.ArticleName))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        // Helper: Parse Excel (using basic CSV-like approach, or add EPPlus for real Excel)
        private async Task<List<ArticleImportDto>> ParseExcelFile(IFormFile file)
        {
            // For Excel, we'll treat it as CSV for now
            // For proper Excel support, install EPPlus NuGet package
            var records = new List<ArticleImportDto>();

            // Try reading as CSV first (Excel can be saved as CSV)
            try
            {
                records = await ParseCsvFile(file);
            }
            catch
            {
                // If fails, you can add EPPlus logic here
                throw new Exception("Excel parsing requires the EPPlus package. Please save your file as CSV or install EPPlus.");
            }

            return records;
        }

        // Helper: Parse CSV line (handles quoted values)
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

        // Helper: Parse decimal safely
        private decimal ParseDecimal(string value)
        {
            value = value.Replace("$", "").Replace(",", "").Trim();
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        // Helper: Import records into database
        private async Task<ImportResult> ImportRecords(List<ArticleImportDto> records)
        {
            var result = new ImportResult
            {
                TotalRows = records.Count,
                Errors = new List<string>()
            };

            var categories = await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

            var existingCodes = await _context.Articles
                .Select(a => a.ArticleCode)
                .ToListAsync();

            var existingCodeSet = new HashSet<string>(existingCodes);

            var rowNumber = 0;
            foreach (var record in records)
            {
                rowNumber++;
                try
                {
                    // Validate
                    if (string.IsNullOrWhiteSpace(record.ArticleName))
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Article name is required.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.CategoryName))
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Row {rowNumber}: Category is required for '{record.ArticleName}'.");
                        continue;
                    }

                    // Generate article code if not provided or duplicate
                    var articleCode = record.ArticleCode;
                    if (string.IsNullOrWhiteSpace(articleCode) || existingCodeSet.Contains(articleCode))
                    {
                        articleCode = await GenerateArticleCode();
                    }

                    // Get or create category
                    var categoryKey = record.CategoryName.ToLower();
                    if (!categories.ContainsKey(categoryKey))
                    {
                        // Create new category
                        var newCategory = new Category
                        {
                            Name = record.CategoryName.Trim(),
                            Description = $"CAT-{record.CategoryName.Substring(0, Math.Min(4, record.CategoryName.Length)).ToUpper()}-{DateTime.Now:MMdd}",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Categories.Add(newCategory);
                        await _context.SaveChangesAsync();
                        categories[categoryKey] = newCategory.Id;
                    }

                    // Get or create unit
                    var unitName = string.IsNullOrWhiteSpace(record.BaseUnit) ? "Piece" : record.BaseUnit.Trim();
                    var unit = await _context.UnitOfMeasures
                        .FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitName.ToLower());

                    if (unit == null)
                    {
                        unit = await _context.UnitOfMeasures.FirstOrDefaultAsync(u => u.UnitName == "Piece")
                               ?? _context.UnitOfMeasures.First();
                    }

                    // Create article
                    var article = new Article
                    {
                        ArticleCode = articleCode,
                        ArticleName = record.ArticleName.Trim(),
                        ArticleType = "Finished",
                        ArticleCategory = categories[categoryKey],
                        ArticleGroup = record.ArticleGroup,
                        Description = record.Description,
                        BaseUnitId = unit.Id,
                        StandardCost = record.StandardCost,
                        StandardPrice = record.StandardPrice,
                        ReorderLevel = record.ReorderLevel,
                        SafetyStock = record.SafetyStock,
                        MaxStockLevel = record.ReorderLevel * 3,
                        TaxRate = record.TaxRate,
                        IsActive = true,
                        IsStockable = true,
                        IsPurchasable = true,
                        IsSellable = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId()
                    };

                    _context.Articles.Add(article);
                    await _context.SaveChangesAsync();

                    // Add barcode if provided
                    if (!string.IsNullOrWhiteSpace(record.PrimaryBarcode))
                    {
                        _context.ArticleBarcodes.Add(new ArticleBarcode
                        {
                            ArticleId = article.Id,
                            Barcode = record.PrimaryBarcode.Trim(),
                            BarcodeType = "CODE128",
                            IsPrimary = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        _context.ArticleBarcodes.Add(new ArticleBarcode
                        {
                            ArticleId = article.Id,
                            Barcode = articleCode,
                            BarcodeType = "CODE128",
                            IsPrimary = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                    existingCodeSet.Add(articleCode);
                    result.SuccessCount++;

                    // Log activity
                    await _activityLogService.LogAsync("Create", "Article", article.Id, null,
                        JsonSerializer.Serialize(new { article.ArticleCode, article.ArticleName }),
                        $"Imported article: {article.ArticleName}");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Row {rowNumber} ({record.ArticleName}): {ex.Message}");
                }
            }

            // Update row numbers in errors
            result.Errors = result.Errors.Select(e =>
            {
                if (e.StartsWith("Row"))
                    return e;
                return e;
            }).ToList();

            return result;
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