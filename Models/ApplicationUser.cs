using Microsoft.AspNetCore.Identity;

namespace Balance.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int CurrentPoints { get; set; } = 0;
    }
}
