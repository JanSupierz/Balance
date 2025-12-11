using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Balance.Models;
using Balance.Data;
using Balance.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Connection ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// --- 2. Identity & Roles ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Adjust password settings here if needed for development
    options.Password.RequireDigit = true;
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// --- 3. Data Access Services ---
// We only register UnitOfWork. It internally manages the specific Repositories.
builder.Services.AddScoped<UnitOfWork>();

// --- 4. MVC & UI ---
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// --- 5. Cookie Configuration ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// --- 6. Database Initialization (Seeding) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // This single line handles Migration + Seeding Users + Seeding Tasks
        await DbInitializer.InitializeAsync(services, context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// --- 7. Middleware Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- 8. Routing ---
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();