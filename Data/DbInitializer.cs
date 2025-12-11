using Balance.Models;
using Balance.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Balance.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            // 1. Ensure Database is Created/Migrated
            await context.Database.MigrateAsync();

            // 2. Seed Identity (Roles & Users)
            await SeedRolesAndUsersAsync(serviceProvider);

            // 3. Seed App Data (Predefined Tasks)
            await SeedPredefinedTasksAsync(context);
        }

        private static async Task SeedRolesAndUsersAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // --- Define roles ---
            string[] roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // --- Create default Admin ---
            string adminEmail = "admin@balance.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // --- Create default User ---
            string userEmail = "user@balance.com";
            var normalUser = await userManager.FindByEmailAsync(userEmail);
            if (normalUser == null)
            {
                normalUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(normalUser, "User123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(normalUser, "User");
            }
        }

        private static async Task SeedPredefinedTasksAsync(ApplicationDbContext context)
        {
            // Check if ANY templates exist
            if (await context.PredefinedTasks.AnyAsync()) return;

            var tasks = new List<PredefinedTask>
            {
                // Daily
                new PredefinedTask { Title = "Drink Water", Description = "Stay hydrated.", Frequency = Frequency.Daily, HowManyTimes = 8, PointsPerClick = 5 },
                new PredefinedTask { Title = "Read 10 Pages", Description = "Expand knowledge.", Frequency = Frequency.Daily, HowManyTimes = 1, PointsPerClick = 15 },
                new PredefinedTask { Title = "Meditate", Description = "Clear your mind.", Frequency = Frequency.Daily, HowManyTimes = 1, PointsPerClick = 20 },
                new PredefinedTask { Title = "Take Vitamins", Description = "Daily supplements.", Frequency = Frequency.Daily, HowManyTimes = 1, PointsPerClick = 10 },

                // Weekly
                new PredefinedTask { Title = "Go to Gym", Description = "Exercise.", Frequency = Frequency.Weekly, HowManyTimes = 3, PointsPerClick = 50 },
                new PredefinedTask { Title = "Grocery Shopping", Description = "Buy food.", Frequency = Frequency.Weekly, HowManyTimes = 1, PointsPerClick = 30 },
                new PredefinedTask { Title = "Laundry", Description = "Wash clothes.", Frequency = Frequency.Weekly, HowManyTimes = 1, PointsPerClick = 25 },
                new PredefinedTask { Title = "Water Plants", Description = "Keep them alive.", Frequency = Frequency.Weekly, HowManyTimes = 1, PointsPerClick = 15 },

                // One Time
                new PredefinedTask { Title = "Dentist Appt", Description = "Checkup.", Frequency = Frequency.OneTime, HowManyTimes = 1, PointsPerClick = 100 },
                new PredefinedTask { Title = "Pay Rent", Description = "Monthly payment.", Frequency = Frequency.OneTime, HowManyTimes = 1, PointsPerClick = 100 },
                new PredefinedTask { Title = "Oil Change", Description = "Car maintenance.", Frequency = Frequency.OneTime, HowManyTimes = 1, PointsPerClick = 50 }
            };

            await context.PredefinedTasks.AddRangeAsync(tasks);
            await context.SaveChangesAsync();
        }
    }
}