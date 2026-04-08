using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store_Management_System.ViewModels
{
    // For Index/List page
    public class CustomerIndexViewModel
    {
        public List<CustomerListViewModel> Customers { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string? CustomerType { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public SelectList CustomerTypes { get; set; }
    }

    // For each row in the list
    public class CustomerListViewModel
    {
        public int Id { get; set; }
        public string ConsigneeCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ConsigneeType { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
        public int OrderCount { get; set; }
    }

    // Base ViewModel for shared properties
    public class BaseCustomerViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Customer type is required")]
        [Display(Name = "Customer Type")]
        public string ConsigneeType { get; set; } = "Individual";
        public SelectList CustomerTypes { get; set; }
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Phone(ErrorMessage = "Invalid mobile number")]
        [StringLength(50, ErrorMessage = "Mobile cannot exceed 50 characters")]
        [Display(Name = "Mobile")]
        public string? Mobile { get; set; }

        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [StringLength(500, ErrorMessage = "Billing address cannot exceed 500 characters")]
        [Display(Name = "Billing Address")]
        public string? BillingAddress { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(50, ErrorMessage = "Tax number cannot exceed 50 characters")]
        [Display(Name = "Tax Number (VAT)")]
        public string? TaxNumber { get; set; }

        [StringLength(100, ErrorMessage = "Payment terms cannot exceed 100 characters")]
        [Display(Name = "Payment Terms")]
        public string? PaymentTerms { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Credit limit must be between 0 and 9,999,999.99")]
        [Display(Name = "Credit Limit")]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Price Level")]
        public string? PriceLevel { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Organization-specific fields
        [Display(Name = "TIN (Tax ID)")]
        public string? TIN { get; set; }

        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [Display(Name = "Number of Employees")]
        public int? NumberOfEmployees { get; set; }

        [Display(Name = "Annual Revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Parent Company")]
        public int? ParentCompanyId { get; set; }

        // Individual-specific fields
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "National ID")]
        public string? NationalId { get; set; }

        [Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        // Contact person (for organizations)
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        // Reserved columns
        [Display(Name = "Reserved 1")]
        public string? Reserved1 { get; set; }

        [Display(Name = "Reserved 2")]
        public string? Reserved2 { get; set; }

        [Display(Name = "Reserved 3")]
        public string? Reserved3 { get; set; }

        [Display(Name = "Reserved 4")]
        public string? Reserved4 { get; set; }

        [Display(Name = "Reserved 5")]
        public string? Reserved5 { get; set; }

        // Dropdowns
        //public SelectList CustomerTypes { get; set; }
        public SelectList PriceLevels { get; set; }
        public SelectList Genders { get; set; }
        public SelectList Industries { get; set; }
        public SelectList ParentCompanies { get; set; }
    }

    // For Create page
    public class CustomerCreateViewModel : BaseCustomerViewModel
    {
        [Display(Name = "Customer Code")]
        public string ConsigneeCode { get; set; } = string.Empty;
    }

    // For Edit page
    public class CustomerEditViewModel : BaseCustomerViewModel
    {
        public int Id { get; set; }
        public string ConsigneeCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CustomerSince { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    // For Details page
    public class CustomerDetailsViewModel : CustomerEditViewModel
    {
        public List<CustomerOrderViewModel> RecentOrders { get; set; } = new();
        public decimal AvailableCredit => (CreditLimit ?? 0) - CurrentBalance;
        public string CreditStatus => AvailableCredit <= 0 ? "Exceeded" : AvailableCredit < (CreditLimit ?? 0) * 0.2m ? "Low" : "Good";
    }

    // For orders on customer details
    public class CustomerOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}