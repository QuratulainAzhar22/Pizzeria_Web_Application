using Microsoft.AspNetCore.Identity;

namespace FastFood.Models
{
    // We inherit from IdentityUser to keep Email, Password, etc.
    // and then add our own custom fields.
    public class ApplicationUser : IdentityUser
    {
        public string? Address { get; set; }
        public string? FavoriteFood { get; set; }
    }
}