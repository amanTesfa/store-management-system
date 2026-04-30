using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Preference
{
    public int Id { get; set; }

    public string PreferenceKey { get; set; } = null!;

    public string? PreferenceValue { get; set; }

    public string ValueType { get; set; } = null!;

    public bool IsSystem { get; set; }

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
