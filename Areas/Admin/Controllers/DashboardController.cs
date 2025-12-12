using Balance.Areas.Admin.ViewModels;
using Balance.Models;
using Balance.Models.Enums;
using Balance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string searchTerm, int page = 1, int pageSize = 10)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                users = users.Where(u =>
                    u.Email.Contains(searchTerm) ||
                    u.UserName.Contains(searchTerm));
            }

            var count = await users.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await users
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new List<UserWithRolesViewModel>();
            foreach (var user in items)
            {
                var roleStrings = await _userManager.GetRolesAsync(user);
                var roles = new List<Role>();
                foreach (var r in roleStrings)
                {
                    if (Enum.TryParse<Role>(r, out var parsedRole))
                    {
                        roles.Add(parsedRole);
                    }
                }

                model.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            ViewBag.Pagination = new PaginationInfo
            {
                SearchTerm = searchTerm,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = count,
                PageSizes = new[] { 5, 10, 20, 50 },
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, Role role, bool addRole)
        {
            // -----------------------------------------------------------
            // 1. SECURITY: Just-In-Time Verification
            // Check the DB immediately to ensure the person clicking the button is still an Admin.
            // -----------------------------------------------------------
            var currentAdminId = _userManager.GetUserId(User);
            var currentAdminUser = await _userManager.FindByIdAsync(currentAdminId);

            if (currentAdminUser == null || !await _userManager.IsInRoleAsync(currentAdminUser, "Admin"))
            {
                // Force logout if they lost admin access in the background
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            // -----------------------------------------------------------

            if (string.IsNullOrWhiteSpace(userId)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Security Check: Prevent removing own Admin role
            // We use currentAdminId which we fetched above
            if (userId == currentAdminId && role == Role.Admin && !addRole)
            {
                TempData["ErrorMessage"] = "Security Alert: You cannot remove your own Administrator role.";
                return RedirectToAction("Index");
            }

            var roleName = role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            IdentityResult result;
            if (addRole)
            {
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    TempData["ErrorMessage"] = $"User is already a {role}.";
                    return RedirectToAction("Index");
                }
                result = await _userManager.AddToRoleAsync(user, roleName);
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    TempData["ErrorMessage"] = $"User is not a {role}.";
                    return RedirectToAction("Index");
                }
                result = await _userManager.RemoveFromRoleAsync(user, roleName);
            }

            if (result.Succeeded)
            {
                // 2. Refresh the TARGET user's security stamp.
                // This ensures their cookie becomes invalid in the DB, so the system picks up the change eventually.
                await _userManager.UpdateSecurityStampAsync(user);

                // 3. If modifying self (adding a role), refresh cookie immediately so UI updates
                if (userId == currentAdminId)
                {
                    await _signInManager.RefreshSignInAsync(user);
                }

                TempData["SuccessMessage"] = $"Updated roles for {user.UserName}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error updating roles.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            // -----------------------------------------------------------
            // 1. SECURITY: Just-In-Time Verification
            // -----------------------------------------------------------
            var currentAdminId = _userManager.GetUserId(User);
            var currentAdminUser = await _userManager.FindByIdAsync(currentAdminId);

            if (currentAdminUser == null || !await _userManager.IsInRoleAsync(currentAdminUser, "Admin"))
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            // -----------------------------------------------------------

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            if (user.UserName == User.Identity.Name)
            {
                TempData["ErrorMessage"] = "You cannot delete your own admin account.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not delete user.";
            }

            return RedirectToAction("Index");
        }
    }
}