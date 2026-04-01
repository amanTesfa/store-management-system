using Microsoft.AspNetCore.Identity;

namespace Store_Management_System.Models
{
    public class RoleClaim : IdentityRoleClaim<int>
    {
        // No additional properties needed
        // IdentityRoleClaim<int> already includes:
        // Id, RoleId, ClaimType, ClaimValue
    }
}