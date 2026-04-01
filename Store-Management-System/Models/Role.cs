using Microsoft.AspNetCore.Identity;

namespace Store_Management_System.Models
{
    public class Role : IdentityRole<int>
    {
        public string? Description { get; set; }
    }
}