using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class NumberingSequence
{
    public int Id { get; set; }

    public string SequenceCode { get; set; } = null!;

    public string SequenceName { get; set; } = null!;

    public string? Prefix { get; set; }

    public string? Suffix { get; set; }

    public int CurrentNumber { get; set; }

    public int NumberLength { get; set; }

    public string? ResetPattern { get; set; }

    public string? ResetValue { get; set; }

    public bool IsActive { get; set; }
}
