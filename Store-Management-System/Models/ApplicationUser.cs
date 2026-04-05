using Microsoft.AspNetCore.Identity;

namespace Store_Management_System.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Custom properties (must match the columns in your Users table)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}