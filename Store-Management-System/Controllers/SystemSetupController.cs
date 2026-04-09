using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.ViewModels;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class SystemSetupController : Controller
    {
        private readonly InventoryDbContext _context;

        public SystemSetupController(InventoryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult UnitCreatePartial() => PartialView("_UnitForm", new UnitEditViewModel());

        [HttpGet]
        public IActionResult FiscalPeriodCreatePartial() => PartialView("_FiscalPeriodForm", new FiscalPeriodEditViewModel());
        // ==================== WAREHOUSES ====================

        [HttpGet]
        public async Task<IActionResult> WarehousesList(string searchTerm = "", int page = 1, int pageSize = 10)
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
                    IsDefault = w.IsDefault,
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

            return PartialView("_WarehousesList", model);
        }

        [HttpGet]
        public IActionResult WarehouseCreatePartial()
        {
            return PartialView("_WarehouseForm", new WarehouseEditViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> WarehouseEditPartial(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WarehouseCreate(WarehouseEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseCode == model.WarehouseCode && !w.IsDeleted);
                if (existing != null)
                    return Json(new { success = false, message = "Warehouse code already exists." });

                var warehouse = new Warehouse
                {
                    WarehouseCode = model.WarehouseCode,
                    WarehouseName = model.WarehouseName,
                    WarehouseType = model.WarehouseType,
                    Location = model.Location,
                    IsDefault = model.IsDefault,
                    IsActive = model.IsActive,
                    ContactPerson = model.ContactPerson,
                    Phone = model.Phone,
                    Email = model.Email,
                    Address = model.Address,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                // Handle IsDefault uniqueness
                if (model.IsDefault)
                {
                    var defaultWarehouses = await _context.Warehouses
                        .Where(w => w.IsDefault && !w.IsDeleted).ToListAsync();
                    foreach (var dw in defaultWarehouses) dw.IsDefault = false;
                }

                _context.Warehouses.Add(warehouse);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' created!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WarehouseEdit(int id, WarehouseEditViewModel model)
        {
            if (id != model.Id) return Json(new { success = false, message = "ID mismatch." });

            if (ModelState.IsValid)
            {
                var warehouse = await _context.Warehouses.FindAsync(id);
                if (warehouse == null) return Json(new { success = false, message = "Warehouse not found." });

                var existing = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseCode == model.WarehouseCode && w.Id != id && !w.IsDeleted);
                if (existing != null)
                    return Json(new { success = false, message = "Warehouse code already exists." });

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

                // Handle IsDefault uniqueness
                if (model.IsDefault)
                {
                    var defaultWarehouses = await _context.Warehouses
                        .Where(w => w.IsDefault && w.Id != id && !w.IsDeleted).ToListAsync();
                    foreach (var dw in defaultWarehouses) dw.IsDefault = false;
                    warehouse.IsDefault = true;
                }
                else
                {
                    warehouse.IsDefault = false;
                }

                _context.Update(warehouse);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' updated!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        [HttpPost]
        public async Task<IActionResult> WarehouseDelete(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.CurrentStocks)
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (warehouse == null)
                return Json(new { success = false, message = "Warehouse not found." });

            if (warehouse.CurrentStocks != null && warehouse.CurrentStocks.Any())
                return Json(new { success = false, message = "Cannot delete warehouse with existing stock." });

            warehouse.IsDeleted = true;
            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Warehouse '{warehouse.WarehouseName}' deleted!" });
        }

        // ==================== UNITS ====================
        
        [HttpGet]
        public async Task<IActionResult> UnitsList(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.UnitOfMeasures
                .Where(u => u.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.UnitCode.Contains(searchTerm) || u.UnitName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var units = await query
                .OrderBy(u => u.UnitCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UnitListViewModel
                {
                    Id = u.Id,
                    UnitCode = u.UnitCode,
                    UnitName = u.UnitName,
                    UnitType = u.UnitType,
                    ConversionFactor = u.ConversionFactor,
                    BaseUnitName = u.BaseUnit != null ? u.BaseUnit.UnitName : null,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            var model = new UnitIndexViewModel
            {
                Units = units,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return PartialView("_UnitsList", model);
        }

        // ==================== FISCAL PERIODS ====================
        
        [HttpGet]
        public async Task<IActionResult> FiscalPeriodsList(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var query = _context.FiscalPeriods.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.PeriodName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var periods = await query
                .OrderByDescending(f => f.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FiscalPeriodListViewModel
                {
                    Id = f.Id,
                    PeriodName = f.PeriodName,
                    StartDate = f.StartDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = f.EndDate.ToDateTime(TimeOnly.MinValue),
                    IsClosed = f.IsClosed
                })
                .ToListAsync();

            var model = new FiscalPeriodIndexViewModel
            {
                Periods = periods,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return PartialView("_FiscalPeriodsList", model);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 1;
        }
    }
}