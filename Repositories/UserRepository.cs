using System.Linq.Expressions;
using Balance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Balance.Repositories
{
    public class UserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserRepository(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        #region Basic CRUD

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync(Expression<Func<ApplicationUser, bool>> filter)
        {
            return await _context.Users.Where(filter).ToListAsync();
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task AddAsync(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await _userManager.CreateAsync(user);
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await _userManager.UpdateAsync(user);
        }

        public async Task DeleteAsync(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await _userManager.DeleteAsync(user);
        }

        #endregion

        #region Search & Pagination

        public async Task<IEnumerable<ApplicationUser>> SearchAsync(string searchTerm, int page, int pageSize)
        {
            return await _context.Users
                .Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm))
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> SearchCountAsync(string searchTerm)
        {
            return await _context.Users
                .Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm))
                .CountAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Users
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        #endregion

        #region Roles Management

        public async Task<IEnumerable<string>> GetRolesAsync(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await _userManager.GetRolesAsync(user);
        }

        public async Task AssignRoleAsync(ApplicationUser user, string role)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentNullException(nameof(role));
            await _userManager.AddToRoleAsync(user, role);
        }

        public async Task RemoveRoleAsync(ApplicationUser user, string role)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentNullException(nameof(role));
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        #endregion
    }
}
