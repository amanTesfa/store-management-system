using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class WarehouseIndexViewModel
    {
        public List<WarehouseListViewModel> Warehouses { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class WarehouseListViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Default Warehouse")]
        public bool IsDefault { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? WarehouseType { get; set; }
        public bool IsActive { get; set; }
        public int StockCount { get; set; }
    }

    public class WarehouseCreateViewModel
    {
        [Required(ErrorMessage = "Warehouse code is required")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        [Display(Name = "Warehouse Code")]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Warehouse name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Warehouse Name")]
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseType { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Default Warehouse")]
        public bool IsDefault { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(100, ErrorMessage = "Contact person cannot exceed 100 characters")]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }
    }

    public class WarehouseEditViewModel : WarehouseCreateViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}