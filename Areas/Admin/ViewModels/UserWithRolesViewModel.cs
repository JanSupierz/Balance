using Balance.Models;
using Balance.Models.Enums;

namespace Balance.Areas.Admin.ViewModels
{
    public class UserWithRolesViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Role> Roles { get; set; }
    }
}