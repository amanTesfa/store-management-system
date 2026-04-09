using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;
using System.Text.RegularExpressions;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class WarehousesController : Controller
    {
        private readonly InventoryDbContext _context;

        public WarehousesController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: Warehouses
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Warehouses
                .Where(w => !w.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(w => w.WarehouseCode.Contains(searchTerm) ||
                                         w.WarehouseName.Contains(searchTerm) ||
                                         (w.Location != null && w.Location.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var warehouses = await query
                .OrderBy(w => w.WarehouseCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WarehouseListViewModel
                {
                    Id = w.Id,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseName = w.WarehouseName,
                    Location = w.Location,
                    WarehouseType = w.WarehouseType,
                    IsActive = w.IsActive,
                    StockCount = _context.CurrentStocks.Count(cs => cs.WarehouseId == w.Id)
                })
                .ToListAsync();

            var model = new WarehouseIndexViewModel
            {
                Warehouses = warehouses,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        // POST: Warehouses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WarehouseCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseCode == model.WarehouseCode && !w.IsDeleted);

                if (existing != null)
                {
                    return Json(new { success = false, message = "Warehouse code already exists." });
                }

                var warehouse = new Warehouse
                {
                    WarehouseCode = model.WarehouseCode,
                    WarehouseName = model.WarehouseName,
                    Location = model.Location,
                    WarehouseType = model.WarehouseType,
                    IsActive = model.IsActive,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Warehouses.Add(warehouse);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' created successfully!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // GET: Warehouses/CreatePartial
        [HttpGet]
        public IActionResult CreatePartial()
        {
            return PartialView("_WarehouseForm", new WarehouseEditViewModel());
        }

        // GET: Warehouses/EditPartial/5
        // GET: Warehouses/EditPartial/5 - Load form for editing
        [HttpGet]
        public async Task<IActionResult> EditPartial(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound();
            }

            var model = new WarehouseEditViewModel
            {
                Id = warehouse.Id,
                WarehouseCode = warehouse.WarehouseCode,
                WarehouseName = warehouse.WarehouseName,
                WarehouseType = warehouse.WarehouseType,
                Location = warehouse.Location,
                IsDefault = warehouse.IsDefault,
                IsActive = warehouse.IsActive,
                ContactPerson = warehouse.ContactPerson,
                Phone = warehouse.Phone,
                Email = warehouse.Email,
                Address = warehouse.Address,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return PartialView("_WarehouseForm", model);
        }

        // POST: Warehouses/Edit/5 - Save edited warehouse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WarehouseEditViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new { success = false, message = "Warehouse ID mismatch." });
            }

            // Remove validation for fields that don't exist in the form
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");

            if (ModelState.IsValid)
            {
                var warehouse = await _context.Warehouses.FindAsync(id);
                if (warehouse == null)
                {
                    return Json(new { success = false, message = "Warehouse not found." });
                }

                // Check if code already exists (excluding current warehouse)
                var existingWarehouse = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseCode == model.WarehouseCode && w.Id != id && !w.IsDeleted);

                if (existingWarehouse != null)
                {
                    return Json(new { success = false, message = "Warehouse code already exists. Please use a unique code." });
                }

                // Update properties
                warehouse.WarehouseCode = model.WarehouseCode;
                warehouse.WarehouseName = model.WarehouseName;
                warehouse.WarehouseType = model.WarehouseType;
                warehouse.Location = model.Location;
                warehouse.IsActive = model.IsActive;
                warehouse.ContactPerson = model.ContactPerson;
                warehouse.Phone = model.Phone;
                warehouse.Email = model.Email;
                warehouse.Address = model.Address;
                warehouse.UpdatedAt = DateTime.UtcNow;
                warehouse.UpdatedBy = GetCurrentUserId();

                // Handle IsDefault - ensure only one default warehouse
                if (model.IsDefault)
                {
                    // Remove default flag from all other warehouses
                    var defaultWarehouses = await _context.Warehouses
                        .Where(w => w.IsDefault && w.Id != id && !w.IsDeleted)
                        .ToListAsync();
                    foreach (var dw in defaultWarehouses)
                    {
                        dw.IsDefault = false;
                    }
                    warehouse.IsDefault = true;
                }
                else
                {
                    warehouse.IsDefault = false;
                }

                _context.Update(warehouse);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' updated successfully!" });
            }

            // If validation failed, return errors
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }
        // POST: Warehouses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.CurrentStocks)
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (warehouse == null)
            {
                return Json(new { success = false, message = "Warehouse not found." });
            }

            if (warehouse.CurrentStocks != null && warehouse.CurrentStocks.Any())
            {
                return Json(new { success = false, message = "Cannot delete warehouse with existing stock. Transfer stock first." });
            }

            warehouse.IsDeleted = true;
            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;
            warehouse.UpdatedBy = GetCurrentUserId();

            _context.Update(warehouse);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' deleted successfully!" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return 1;
        }
    }
}