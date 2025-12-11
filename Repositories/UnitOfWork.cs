using Balance.Models;
using Microsoft.AspNetCore.Identity;

namespace Balance.Repositories
{
    public class UnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;

            Users = new UserRepository(userManager, context);
            RepeatedTasks = new Repository<UserTask>(context);
            PredefinedTasks = new Repository<PredefinedTask>(context);
            TaskTags = new Repository<TaskTag>(context);
            Prizes = new Repository<Prize>(context);
        }

        public UserRepository Users { get; }
        public Repository<UserTask> RepeatedTasks { get; }
        public Repository<PredefinedTask> PredefinedTasks { get; }
        public Repository<TaskTag> TaskTags { get; }
        public Repository<Prize> Prizes { get; }

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}