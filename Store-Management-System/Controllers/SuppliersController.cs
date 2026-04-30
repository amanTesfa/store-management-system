using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.Json;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class SuppliersController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public SuppliersController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;   
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Suppliers
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s => s.Name.Contains(searchTerm) ||
                                         (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)) ||
                                         (s.Email != null && s.Email.Contains(searchTerm)) ||
                                         (s.Phone != null && s.Phone.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var suppliers = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SupplierListViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    ContactPerson = s.ContactPerson,
                    Email = s.Email,
                    Phone = s.Phone,
                    CreditLimit = s.CreditLimit,
                    IsActive = s.IsActive,
                    Rating = s.Rating ?? 0,
                    ArticleCount = _context.Articles.Count(a => a.SupplierId == s.Id && a.IsActive)
                })
                .ToListAsync();

            var model = new SupplierIndexViewModel
            {
                Suppliers = suppliers,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: Suppliers/CreatePartial
        [HttpGet]
        public async Task<IActionResult> CreatePartial()
        {
            return PartialView("CreatePartial", new SupplierCreateViewModel());
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate name
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Name == model.Name && !s.IsDeleted);

                if (existingSupplier != null)
                {
                    return Json(new { success = false, message = "A supplier with this name already exists." });
                }

                var supplier = new Supplier
                {
                    Name = model.Name,
                    ContactPerson = model.ContactPerson,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.Address,
                    PaymentTerms = model.PaymentTerms,
                    CreditLimit = model.CreditLimit,
                    IsActive = model.IsActive,
                    IsDeleted = false,
                    Rating = 0, // Initial rating
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Supplier '{supplier.Name}' created successfully!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // GET: Suppliers/DetailsPartial/5
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (supplier == null)
            {
                return NotFound();
            }

            // Get articles from this supplier
            var articles = await _context.Articles
                .Where(a => a.SupplierId == id && a.IsActive)
                .Select(a => new SupplierArticleViewModel
                {
                    Id = a.Id,
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.ArticleName,
                    StandardPrice = a.StandardPrice,
                    CurrentStock = 0, // You can calculate from stock movements
                    IsActive = a.IsActive
                })
                .ToListAsync();

            var model = new SupplierDetailsViewModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                PaymentTerms = supplier.PaymentTerms,
                CreditLimit = supplier.CreditLimit,
                IsActive = supplier.IsActive,
                Rating = supplier.Rating ?? 0,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt,
                Articles = articles
            };

            return PartialView("DetailsPartial", model);
        }

        // GET: Suppliers/EditPartial/5
        [HttpGet]
        public async Task<IActionResult> EditPartial(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (supplier == null)
            {
                return NotFound();
            }

            var model = new SupplierEditViewModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                PaymentTerms = supplier.PaymentTerms,
                CreditLimit = supplier.CreditLimit,
                IsActive = supplier.IsActive,
                Rating = supplier.Rating ?? 0,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt
            };

            return PartialView("EditPartial", model);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierEditViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new { success = false, message = "Supplier ID mismatch." });
            }

            if (ModelState.IsValid)
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    return Json(new { success = false, message = "Supplier not found." });
                }

                // Check for duplicate name (excluding current)
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Name == model.Name && s.Id != id && !s.IsDeleted);

                if (existingSupplier != null)
                {
                    return Json(new { success = false, message = "A supplier with this name already exists." });
                }

                supplier.Name = model.Name;
                supplier.ContactPerson = model.ContactPerson;
                supplier.Email = model.Email;
                supplier.Phone = model.Phone;
                supplier.Address = model.Address;
                supplier.PaymentTerms = model.PaymentTerms;
                supplier.CreditLimit = model.CreditLimit;
                supplier.IsActive = model.IsActive;
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.UpdatedBy = GetCurrentUserId();

                _context.Update(supplier);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Edit", "Supplier", supplier.Id, details: $"Updated supplier '{supplier.Name}'");
                // Recalculate rating based on articles? (Optional)
                await UpdateSupplierRating(id);

                return Json(new { success = true, message = $"Supplier '{supplier.Name}' updated successfully!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Articles)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found." });
            }

            // Check if supplier has active articles
            var activeArticlesCount = supplier.Articles?.Count(a => a.IsActive) ?? 0;
            if (activeArticlesCount > 0)
            {
                return Json(new { success = false, message = $"Cannot delete supplier '{supplier.Name}' because it has {activeArticlesCount} active articles. Please reassign or delete the articles first." });
            }

            // Soft delete
            supplier.IsDeleted = true;
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedBy = GetCurrentUserId();

            _context.Update(supplier);
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("Delete", "Supplier", supplier.Id, details: $"Deleted supplier '{supplier.Name}'");
            return Json(new { success = true, message = $"Supplier '{supplier.Name}' has been deleted successfully!" });
        }

        // Helper: Update supplier rating based on article performance
        private async Task UpdateSupplierRating(int supplierId)
        {
            // Example: Calculate average rating based on article sales or quality
            // For now, keep as is or implement custom logic
            var articleCount = await _context.Articles.CountAsync(a => a.SupplierId == supplierId && a.IsActive);

            // Simple logic: More articles = higher rating (1-5 scale)
            int newRating = 0;
            if (articleCount >= 50) newRating = 5;
            else if (articleCount >= 30) newRating = 4;
            else if (articleCount >= 15) newRating = 3;
            else if (articleCount >= 5) newRating = 2;
            else if (articleCount > 0) newRating = 1;

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier != null && supplier.Rating != newRating)
            {
                supplier.Rating = newRating;
                supplier.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Helper: Get current user ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 1;
        }
    }
}