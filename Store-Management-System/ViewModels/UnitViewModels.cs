using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class UnitIndexViewModel
    {
        public List<UnitListViewModel> Units { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class UnitListViewModel
    {
        public int Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
        public string? BaseUnitName { get; set; }
        public bool IsActive { get; set; }
    }

    public class UnitCreateViewModel
    {
        [Required(ErrorMessage = "Unit code is required")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        [Display(Name = "Unit Code")]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Display(Name = "Unit Name")]
        public string UnitName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit type is required")]
        [Display(Name = "Unit Type")]
        public string UnitType { get; set; } = "Base";

        [Display(Name = "Base Unit")]
        public int? BaseUnitId { get; set; }

        [Display(Name = "Conversion Factor")]
        public decimal ConversionFactor { get; set; } = 1;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public SelectList BaseUnits { get; set; }
    }

    public class UnitEditViewModel : UnitCreateViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}