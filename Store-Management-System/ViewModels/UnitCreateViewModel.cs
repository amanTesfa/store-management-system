using System.ComponentModel.DataAnnotations;

public class UnitCreateViewModel
{
    // ... similar to Warehouse

    [Display(Name = "Unit Type")]
    public string UnitType { get; set; } = "Base"; // Base or Conversion

    [Display(Name = "Base Unit")]
    public int? BaseUnitId { get; set; } // For conversion units

    [Display(Name = "Conversion Factor")]
    public decimal ConversionFactor { get; set; } = 1;
}