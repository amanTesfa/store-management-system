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
    public class CustomersController : Controller
    {
        private readonly InventoryDbContext _context;

        public CustomersController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index(string searchTerm = "", string customerType = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Consignees
                .Where(c => c.IsActive)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.ConsigneeCode.Contains(searchTerm) ||
                                         c.ConsigneeName.Contains(searchTerm) ||
                                         (c.Email != null && c.Email.Contains(searchTerm)) ||
                                         (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                                         (c.Tin != null && c.Tin.Contains(searchTerm)));
            }

            // Filter by customer type
            if (!string.IsNullOrWhiteSpace(customerType))
            {
                query = query.Where(c => c.ConsigneeType == customerType);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var customers = await query
                .OrderBy(c => c.ConsigneeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerListViewModel
                {
                    Id = c.Id,
                    ConsigneeCode = c.ConsigneeCode,
                    DisplayName = c.ConsigneeName,  // Map ConsigneeName to DisplayName
                    ConsigneeType = c.ConsigneeType,
                    Email = c.Email,
                    Phone = c.Phone,
                    CreditLimit = c.CreditLimit,
                    CurrentBalance = c.CurrentBalance,
                    IsActive = c.IsActive,
                    OrderCount = _context.Orders.Count(o => o.CreatedBy == c.Id)
                })
                .ToListAsync();

            // Setup dropdowns
            var customerTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- All Types --" },
                new SelectListItem { Value = "Individual", Text = "Individual" },
                new SelectListItem { Value = "Organization", Text = "Organization" }
            };

            var model = new CustomerIndexViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm ?? string.Empty,
                CustomerType = customerType,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                CustomerTypes = new SelectList(customerTypes, "Value", "Text", customerType)
            };

            return View(model);
        }

        // GET: Customers/CreatePartial
        [HttpGet]
        public async Task<IActionResult> CreatePartial()
        {
            var model = new CustomerCreateViewModel
            {
                ConsigneeCode = await GenerateCustomerCode(),
                ConsigneeType = "Individual"
            };
            await PopulateDropdowns(model);
            return PartialView("CreatePartial", model);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            // Remove dropdowns from validation
            ModelState.Remove("CustomerTypes");
            ModelState.Remove("PriceLevels");
            ModelState.Remove("Genders");
            ModelState.Remove("Industries");
            ModelState.Remove("ParentCompanies");

            if (ModelState.IsValid)
            {
                // Check for duplicate code
                var existingCustomer = await _context.Consignees
                    .FirstOrDefaultAsync(c => c.ConsigneeCode == model.ConsigneeCode);

                if (existingCustomer != null)
                {
                    return Json(new { success = false, message = "A customer with this code already exists." });
                }

                var customer = new Consignee
                {
                    ConsigneeCode = model.ConsigneeCode,
                    ConsigneeType = model.ConsigneeType,
                    ConsigneeName = model.DisplayName,  // Map DisplayName to ConsigneeName
                    Email = model.Email,
                    Phone = model.Phone,
                    Mobile = model.Mobile,
                    ShippingAddress = model.ShippingAddress,
                    BillingAddress = model.BillingAddress,
                    City = model.City,
                    State = model.State,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    TaxNumber = model.TaxNumber,
                    PaymentTerms = model.PaymentTerms,
                    CreditLimit = model.CreditLimit,
                    PriceLevel = model.PriceLevel,
                    IsActive = model.IsActive,
                    CustomerSince = DateOnly.FromDateTime(DateTime.UtcNow),
                    CurrentBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId(),

                    // Organization fields
                    Tin = model.TIN,
                    Industry = model.Industry,
                    NumberOfEmployees = model.NumberOfEmployees,
                    AnnualRevenue = model.AnnualRevenue,
                    Website = model.Website,
                    ParentCompanyId = model.ParentCompanyId,
                    ContactPerson = model.ContactPerson,

                    // Individual fields
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null,
                    Gender = model.Gender,
                    NationalId = model.NationalId,
                    Occupation = model.Occupation,

                    // Reserved
                    Reserved1 = model.Reserved1,
                    Reserved2 = model.Reserved2,
                    Reserved3 = model.Reserved3,
                    Reserved4 = model.Reserved4,
                    Reserved5 = model.Reserved5
                };

                _context.Consignees.Add(customer);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Customer '{customer.ConsigneeName}' created successfully!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // GET: Customers/DetailsPartial/5
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var customer = await _context.Consignees
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            // Get recent orders
            var recentOrders = await _context.Orders
                .Where(o => o.CreatedBy == id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new CustomerOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            var model = new CustomerDetailsViewModel
            {
                Id = customer.Id,
                ConsigneeCode = customer.ConsigneeCode,
                ConsigneeType = customer.ConsigneeType,
                DisplayName = customer.ConsigneeName,  // Map ConsigneeName to DisplayName
                Email = customer.Email,
                Phone = customer.Phone,
                Mobile = customer.Mobile,
                ShippingAddress = customer.ShippingAddress,
                BillingAddress = customer.BillingAddress,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                TaxNumber = customer.TaxNumber,
                PaymentTerms = customer.PaymentTerms,
                CreditLimit = customer.CreditLimit,
                CurrentBalance = customer.CurrentBalance,
                PriceLevel = customer.PriceLevel,
                IsActive = customer.IsActive,
                CustomerSince = customer.CustomerSince.HasValue ? customer.CustomerSince.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,

                TIN = customer.Tin,
                Industry = customer.Industry,
                NumberOfEmployees = customer.NumberOfEmployees,
                AnnualRevenue = customer.AnnualRevenue,
                Website = customer.Website,
                ParentCompanyId = customer.ParentCompanyId,
                ContactPerson = customer.ContactPerson,

                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth.HasValue ? customer.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                Gender = customer.Gender,
                NationalId = customer.NationalId,
                Occupation = customer.Occupation,

                Reserved1 = customer.Reserved1,
                Reserved2 = customer.Reserved2,
                Reserved3 = customer.Reserved3,
                Reserved4 = customer.Reserved4,
                Reserved5 = customer.Reserved5,

                RecentOrders = recentOrders
            };

            return PartialView("DetailsPartial", model);
        }

        // GET: Customers/EditPartial/5
        [HttpGet]
        public async Task<IActionResult> EditPartial(int id)
        {
            var customer = await _context.Consignees
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerEditViewModel
            {
                Id = customer.Id,
                ConsigneeCode = customer.ConsigneeCode,
                ConsigneeType = customer.ConsigneeType,
                DisplayName = customer.ConsigneeName,  // Map ConsigneeName to DisplayName
                Email = customer.Email,
                Phone = customer.Phone,
                Mobile = customer.Mobile,
                ShippingAddress = customer.ShippingAddress,
                BillingAddress = customer.BillingAddress,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                TaxNumber = customer.TaxNumber,
                PaymentTerms = customer.PaymentTerms,
                CreditLimit = customer.CreditLimit,
                CurrentBalance = customer.CurrentBalance,
                PriceLevel = customer.PriceLevel,
                IsActive = customer.IsActive,
                CustomerSince = customer.CustomerSince.HasValue ? customer.CustomerSince.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,

                TIN = customer.Tin,
                Industry = customer.Industry,
                NumberOfEmployees = customer.NumberOfEmployees,
                AnnualRevenue = customer.AnnualRevenue,
                Website = customer.Website,
                ParentCompanyId = customer.ParentCompanyId,
                ContactPerson = customer.ContactPerson,

                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth.HasValue ? customer.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                Gender = customer.Gender,
                NationalId = customer.NationalId,
                Occupation = customer.Occupation,

                Reserved1 = customer.Reserved1,
                Reserved2 = customer.Reserved2,
                Reserved3 = customer.Reserved3,
                Reserved4 = customer.Reserved4,
                Reserved5 = customer.Reserved5
            };

            await PopulateDropdowns(model);
            return PartialView("EditPartial", model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerEditViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new { success = false, message = "Customer ID mismatch." });
            }

            // Remove dropdowns from validation
            ModelState.Remove("CustomerTypes");
            ModelState.Remove("PriceLevels");
            ModelState.Remove("Genders");
            ModelState.Remove("Industries");
            ModelState.Remove("ParentCompanies");

            if (ModelState.IsValid)
            {
                var customer = await _context.Consignees.FindAsync(id);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." });
                }

                // Update fields
                customer.ConsigneeType = model.ConsigneeType;
                customer.ConsigneeName = model.DisplayName;  // Map DisplayName to ConsigneeName
                customer.Email = model.Email;
                customer.Phone = model.Phone;
                customer.Mobile = model.Mobile;
                customer.ShippingAddress = model.ShippingAddress;
                customer.BillingAddress = model.BillingAddress;
                customer.City = model.City;
                customer.State = model.State;
                customer.PostalCode = model.PostalCode;
                customer.Country = model.Country;
                customer.TaxNumber = model.TaxNumber;
                customer.PaymentTerms = model.PaymentTerms;
                customer.CreditLimit = model.CreditLimit;
                customer.PriceLevel = model.PriceLevel;
                customer.IsActive = model.IsActive;
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = GetCurrentUserId();

                // Organization fields
                customer.Tin = model.TIN;
                customer.Industry = model.Industry;
                customer.NumberOfEmployees = model.NumberOfEmployees;
                customer.AnnualRevenue = model.AnnualRevenue;
                customer.Website = model.Website;
                customer.ParentCompanyId = model.ParentCompanyId;
                customer.ContactPerson = model.ContactPerson;

                // Individual fields
                customer.FirstName = model.FirstName;
                customer.LastName = model.LastName;
                customer.DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null;
                customer.Gender = model.Gender;
                customer.NationalId = model.NationalId;
                customer.Occupation = model.Occupation;

                // Reserved
                customer.Reserved1 = model.Reserved1;
                customer.Reserved2 = model.Reserved2;
                customer.Reserved3 = model.Reserved3;
                customer.Reserved4 = model.Reserved4;
                customer.Reserved5 = model.Reserved5;

                _context.Update(customer);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Customer '{customer.ConsigneeName}' updated successfully!" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Consignees
                .Include(c => c.Vouchers)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            // Check if customer has active orders
            var activeOrdersCount = customer.Vouchers?.Count(o => o.Status != "Cancelled" && o.Status != "Completed") ?? 0;
            if (activeOrdersCount > 0)
            {
                return Json(new { success = false, message = $"Cannot delete customer '{customer.ConsigneeName}' because they have {activeOrdersCount} active orders." });
            }

            // Soft delete
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = GetCurrentUserId();

            _context.Update(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Customer '{customer.ConsigneeName}' has been deleted successfully!" });
        }

        // Helper: Generate customer code
        private async Task<string> GenerateCustomerCode()
        {
            string prefix = "CUST-";
            var lastCustomer = await _context.Consignees
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCustomer != null && lastCustomer.ConsigneeCode.StartsWith(prefix))
            {
                var match = Regex.Match(lastCustomer.ConsigneeCode, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D6}";
        }
        // GET: Customers/StatementPartial/5
        [HttpGet]
        public async Task<IActionResult> StatementPartial(int id, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var customer = await _context.Consignees
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            // Set default date range (last 3 months if not specified)
            if (!fromDate.HasValue)
                fromDate = DateTime.UtcNow.AddMonths(-3);
            if (!toDate.HasValue)
                toDate = DateTime.UtcNow;

            // Convert DateTime to DateOnly for database comparison
            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
            var toDateOnly = DateOnly.FromDateTime(toDate.Value);

            // Get all vouchers for this customer (invoices, payments, credit notes, returns)
            var transactions = await _context.Vouchers
                .Where(v => v.ConsigneeId == id && v.VoucherDate >= fromDateOnly && v.VoucherDate <= toDateOnly)
                .OrderBy(v => v.VoucherDate)
                .Select(v => new CustomerStatementTransactionViewModel
                {
                    Date = v.VoucherDate.ToDateTime(TimeOnly.MinValue),
                    Reference = v.VoucherNumber,
                    Description = v.VoucherType,
                    Debit = v.VoucherType == "Invoice" ? v.TotalAmount : 0,
                    Credit = (v.VoucherType == "Payment" || v.VoucherType == "CreditNote") ? v.TotalAmount : 0,
                    VoucherId = v.Id
                })
                .ToListAsync();

            // Calculate running balance
            decimal runningBalance = 0;
            foreach (var transaction in transactions)
            {
                runningBalance += transaction.Debit - transaction.Credit;
                transaction.Balance = runningBalance;
            }

            // Get opening balance (transactions before fromDate)
            var openingBalance = await _context.Vouchers
                .Where(v => v.ConsigneeId == id && v.VoucherDate < fromDateOnly)
                .SumAsync(v => v.VoucherType == "Invoice" ? v.TotalAmount :
                              (v.VoucherType == "Payment" || v.VoucherType == "CreditNote") ? -v.TotalAmount : 0);

            var model = new CustomerStatementViewModel
            {
                CustomerId = customer.Id,
                CustomerName = customer.ConsigneeName,
                CustomerCode = customer.ConsigneeCode,
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                Transactions = transactions,
                ClosingBalance = runningBalance + openingBalance
            };

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return PartialView("StatementPartial", model);
        }
        // Helper: Populate dropdowns
        private async Task PopulateDropdowns(BaseCustomerViewModel model)
        {
            // Price Levels
            var priceLevels = new List<SelectListItem>
            {
                new SelectListItem { Value = "Retail", Text = "Retail" },
                new SelectListItem { Value = "Wholesale", Text = "Wholesale" },
                new SelectListItem { Value = "VIP", Text = "VIP" },
                new SelectListItem { Value = "Distributor", Text = "Distributor" }
            };
            model.PriceLevels = new SelectList(priceLevels, "Value", "Text", model.PriceLevel);

            // Genders
            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" },
                new SelectListItem { Value = "Prefer not to say", Text = "Prefer not to say" }
            };
            model.Genders = new SelectList(genders, "Value", "Text", model.Gender);

            // Industries
            var industries = new List<SelectListItem>
            {
                new SelectListItem { Value = "Retail", Text = "Retail" },
                new SelectListItem { Value = "Manufacturing", Text = "Manufacturing" },
                new SelectListItem { Value = "Technology", Text = "Technology" },
                new SelectListItem { Value = "Healthcare", Text = "Healthcare" },
                new SelectListItem { Value = "Education", Text = "Education" },
                new SelectListItem { Value = "Finance", Text = "Finance" },
                new SelectListItem { Value = "Hospitality", Text = "Hospitality" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
            model.Industries = new SelectList(industries, "Value", "Text", model.Industry);

            // Parent Companies (for organizations)
            var parentCompanies = await _context.Consignees
                .Where(c => c.ConsigneeType == "Organization" && c.IsActive && (model.Id == 0 || c.Id != model.Id))
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ConsigneeName })
                .ToListAsync();
            parentCompanies.Insert(0, new SelectListItem { Value = "", Text = "-- None --" });
            model.ParentCompanies = new SelectList(parentCompanies, "Value", "Text", model.ParentCompanyId?.ToString());
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