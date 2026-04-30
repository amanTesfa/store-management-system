using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class SystemConstant
{
    public int Id { get; set; }

    public string ConstantGroup { get; set; } = null!;

    public string ConstantCode { get; set; } = null!;

    public string ConstantName { get; set; } = null!;

    public string? ConstantValue { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public string? Description { get; set; }
}
