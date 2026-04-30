using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;
using System.Text.Json;
using Store_Management_System.DTOs;
using Store_Management_System.Extensions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CategoriesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ActivityLogService _activityLogService;
        public CategoriesController(InventoryDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // GET: Categories
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Include(c => c.InverseParent)
                .ToListAsync();

            // Build tree structure
            var tree = BuildCategoryTree(categories, null, 0, searchTerm);

            var model = new CategoryIndexViewModel
            {
                Categories = tree,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            await PopulateParentCategories();
            return View(new CategoryCreateViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {

            // Remove the SelectList from validation (it's not submitted)
            ModelState.Remove("ParentCategories");

            // Check for duplicate name at same level
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == model.Name && c.ParentId == model.ParentId && !c.IsDeleted);

            if (existingCategory != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists at the same level.");
            }

            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ParentId = model.ParentId,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };
            

                await _context.SaveChangesAsync();
                // Return JSON for AJAX request
                return Json(new { success = true, message = $"Category '{category.Name}' created successfully!" });
            }

            // Return validation errors as JSON
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            await PopulateParentCategories(category.ParentId, category.Id);
            return View(model);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
        {
            // Remove the SelectList from validation (it's not submitted)
            ModelState.Remove("ParentCategories");

            if (id != model.Id)
            {
                return Json(new { success = false, message = "Category ID mismatch." });
            }

            // Check for duplicate name at same level (excluding current)
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == model.Name && c.ParentId == model.ParentId && c.Id != id && !c.IsDeleted);

            if (existingCategory != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists at the same level.");
            }

            // Prevent circular reference
            if (model.ParentId.HasValue && IsCircularReference(model.ParentId.Value, id))
            {
                ModelState.AddModelError("ParentId", "Cannot set a child category as parent (circular reference).");
            }

            if (ModelState.IsValid)
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.ParentId = model.ParentId;
                category.DisplayOrder = model.DisplayOrder;
                category.IsActive = model.IsActive;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = GetCurrentUserId();

                _context.Update(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Category '{category.Name}' updated successfully!" });
            }

            // Return validation errors as JSON
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }
        // GET: Categories/TreePartial
        [HttpGet]
        public async Task<IActionResult> TreePartial(string searchTerm = "")
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Include(c => c.InverseParent)
                .ToListAsync();

            var tree = BuildCategoryTree(categories, null, 0, searchTerm);
            ViewBag.Categories = tree;
            return PartialView("TreePartial");
        }

        // GET: Categories/DetailsPartial
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
            {
                return NotFound();
            }

            var articleCount = await _context.Articles.CountAsync(a => a.ArticleCategory == id && a.IsActive);
            var parentName = category.ParentId.HasValue
                ? (await _context.Categories.FindAsync(category.ParentId.Value))?.Name
                : null;

            ViewBag.ArticleCount = articleCount;
            ViewBag.ParentName = parentName;

            return PartialView("DetailsPartial", category);
        }

        // GET: Categories/CreatePartial
        [HttpGet]
        public async Task<IActionResult> CreatePartial()
        {
            await PopulateParentCategories();
            return PartialView("CreatePartial", new CategoryCreateViewModel());
        }

        // GET: Categories/EditPartial
        [HttpGet]
        public async Task<IActionResult> EditPartial(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            await PopulateParentCategories(category.ParentId, category.Id);
            return PartialView("EditPartial", model);
        }
        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.InverseParent)
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            // Check if category has child categories
            if (category.InverseParent != null && category.InverseParent.Any())
            {
                return Json(new { success = false, message = "Cannot delete category with sub-categories. Please delete or move sub-categories first." });
            }

            // Check if category has articles
            if (category.Articles != null && category.Articles.Any(a => a.IsActive))
            {
                var activeArticleCount = category.Articles.Count(a => a.IsActive);
                return Json(new { success = false, message = $"Cannot delete category '{category.Name}' because it has {activeArticleCount} active articles. Please reassign or delete the articles first." });
            }

            // Soft delete
            category.IsDeleted = true;
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = GetCurrentUserId();

            _context.Update(category);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Category '{category.Name}' has been deleted successfully!" });
        }
        // POST: Categories/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder([FromBody] List<CategoryReorderModel> reorderData)
        {
            try
            {
                foreach (var item in reorderData)
                {
                    var category = await _context.Categories.FindAsync(item.Id);
                    if (category != null)
                    {
                        category.ParentId = item.ParentId;
                        category.DisplayOrder = item.DisplayOrder;
                        category.UpdatedAt = DateTime.UtcNow;
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Categories reordered successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Categories/CheckDelete
        [HttpGet]
        public async Task<IActionResult> CheckDelete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.InverseParent)
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
            {
                return Json(new { canDelete = false, message = "Category not found." });
            }

            var hasChildren = category.InverseParent != null && category.InverseParent.Any();
            var articleCount = category.Articles?.Count(a => a.IsActive) ?? 0;

            if (hasChildren)
            {
                return Json(new { canDelete = false, message = "Cannot delete category with sub-categories." });
            }

            if (articleCount > 0)
            {
                return Json(new { canDelete = false, message = $"Cannot delete category with {articleCount} articles." });
            }

            return Json(new { canDelete = true, message = "" });
        }

        // Helper: Build category tree
        private List<CategoryTreeNodeViewModel> BuildCategoryTree(List<Category> allCategories, int? parentId, int level, string searchTerm)
        {
            var categories = allCategories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            var result = new List<CategoryTreeNodeViewModel>();

            foreach (var category in categories)
            {
                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm) &&
                    !category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    var children = BuildCategoryTree(allCategories, category.Id, level + 1, searchTerm);
                    if (!children.Any())
                    {
                        continue;
                    }
                }

                var articleCount = _context.Articles.Count(a => a.ArticleCategory == category.Id && a.IsActive);
                var childCount = allCategories.Count(c => c.ParentId == category.Id && !c.IsDeleted);

                var node = new CategoryTreeNodeViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    ParentId = category.ParentId,
                    DisplayOrder = category.DisplayOrder,
                    IsActive = category.IsActive,
                    ChildCount = childCount,
                    ArticleCount = articleCount,
                    Level = level,
                    Children = BuildCategoryTree(allCategories, category.Id, level + 1, searchTerm)
                };

                result.Add(node);
            }

            return result;
        }

        // Helper: Check for circular reference
        private bool IsCircularReference(int parentId, int childId)
        {
            var parent = _context.Categories.Find(parentId);
            while (parent != null)
            {
                if (parent.Id == childId)
                    return true;
                parent = _context.Categories.Find(parent.ParentId);
            }
            return false;
        }

        // Helper: Populate parent categories dropdown
        private async Task PopulateParentCategories(int? selectedId = null, int? excludeId = null)
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted && (excludeId == null || c.Id != excludeId))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var categoryList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- No Parent (Root Category) --" }
            };

            foreach (var category in categories)
            {
                categoryList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = GetCategoryIndent(category, categories),
                    Selected = selectedId == category.Id
                });
            }

            ViewBag.ParentCategories = new SelectList(categoryList, "Value", "Text", selectedId);
        }

        // Helper: Get indented category name for dropdown
        private string GetCategoryIndent(Category category, List<Category> allCategories, string indent = "")
        {
            var result = indent + category.Name;
            if (category.ParentId.HasValue)
            {
                var parent = allCategories.FirstOrDefault(c => c.Id == category.ParentId);
                if (parent != null)
                {
                    return GetCategoryIndent(parent, allCategories, "— ") + " → " + category.Name;
                }
            }
            return result;
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