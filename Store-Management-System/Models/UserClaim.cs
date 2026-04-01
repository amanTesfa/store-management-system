using Microsoft.AspNetCore.Identity;

namespace StoreManagementSystem.Models
{
    public class UserClaim : IdentityUserClaim<int>
    {
        // No additional properties needed
        // IdentityUserClaim<int> already includes:
        // Id, UserId, ClaimType, ClaimValue
    }
}