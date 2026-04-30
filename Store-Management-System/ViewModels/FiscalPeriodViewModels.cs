using System.ComponentModel.DataAnnotations;

namespace Store_Management_System.ViewModels
{
    public class FiscalPeriodIndexViewModel
    {
        public List<FiscalPeriodListViewModel> Periods { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class FiscalPeriodListViewModel
    {
        public int Id { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; }
    }

    public class FiscalPeriodCreateViewModel
    {
        [Required(ErrorMessage = "Period name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Display(Name = "Period Name")]
        public string PeriodName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Is Closed")]
        public bool IsClosed { get; set; }
        public bool IsActive { get; set; }
    }

    public class FiscalPeriodEditViewModel : FiscalPeriodCreateViewModel
    {
        public int Id { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public string PeriodType { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}