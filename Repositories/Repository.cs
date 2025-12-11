using System.Linq.Expressions;
using Balance.Models;
using Microsoft.EntityFrameworkCore;

namespace Balance.Repositories
{
    public class Repository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // --- THE BIG CHANGE IS HERE ---
        // We combined your two methods into one flexible method.
        // It now accepts an optional filter AND an optional list of properties to include (e.g., "Tags").
        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            // 1. Apply Filter (if provided)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Apply Includes (if provided)
            // This allows you to say "Give me Tasks AND their Tags"
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id) => await _dbSet.FindAsync(id);

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public async Task DeleteAsync(object id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null) _dbSet.Remove(entity);
        }

        public async Task<int> CountAsync() => await _dbSet.CountAsync();
    }
}