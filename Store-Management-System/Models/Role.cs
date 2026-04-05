using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Role: IdentityRole<int>
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string? ConcurrencyStamp { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
