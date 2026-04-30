using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Activity
{
    public int Id { get; set; }

    public string ActivityCode { get; set; } = null!;

    public string ActivityName { get; set; } = null!;

    public string ActivityType { get; set; } = null!;

    public string Direction { get; set; } = null!;

    public bool AffectsStock { get; set; }

    public bool AffectsAccounting { get; set; }

    public bool IsActive { get; set; }

    public int? DefaultVoucherTypeId { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
