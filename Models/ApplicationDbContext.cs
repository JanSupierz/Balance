using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Balance.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public DbSet<PredefinedTask> PredefinedTasks { get; set; } = default!;

        public DbSet<UserTask> UserTasks { get; set; } = default!;

        public DbSet<Prize> Prizes { get; set; }

        public DbSet<TaskTag> TaskTags { get; set; }
    }
}