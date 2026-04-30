using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store_Management_System.ViewModels
{
    // For Index/List page
    public class SupplierIndexViewModel
    {
        public List<SupplierListViewModel> Suppliers { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    // For each row in the list
    public class SupplierListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public int Rating { get; set; }
        public int ArticleCount { get; set; }
    }

    // For Create page
    public class SupplierCreateViewModel
    {
        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200, ErrorMessage = "Supplier name cannot exceed 200 characters")]
        [Display(Name = "Supplier Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Contact person name cannot exceed 100 characters")]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Payment terms cannot exceed 100 characters")]
        [Display(Name = "Payment Terms")]
        public string? PaymentTerms { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Credit limit must be between 0 and 9,999,999.99")]
        [Display(Name = "Credit Limit")]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }

    // For Edit page
    public class SupplierEditViewModel : SupplierCreateViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Rating { get; set; }
    }

    // For Details page
    public class SupplierDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SupplierArticleViewModel> Articles { get; set; } = new();
    }

    // For articles under supplier
    public class SupplierArticleViewModel
    {
        public int Id { get; set; }
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
        public int CurrentStock { get; set; }
        public bool IsActive { get; set; }
    }
}