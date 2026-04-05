using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Store_Management_System.Models
{
    public class AppIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Map Identity tables to your existing table names (clean names)
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        }
    }
}