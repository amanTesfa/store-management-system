using System.ComponentModel.DataAnnotations;

public class FiscalPeriodCreateViewModel
{
    [Required]
    [Display(Name = "Period Name")]
    public string PeriodName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Is Closed")]
    public bool IsClosed { get; set; }
}