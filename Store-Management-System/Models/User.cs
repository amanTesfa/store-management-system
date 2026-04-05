using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Store_Management_System.Models
{
    public class User : IdentityUser<int>
    {
        // Custom properties only
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
        public virtual ICollection<UserLogin> UserLogins { get; set; } = new List<UserLogin>();
        public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}