using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure DbContext - Database First Approach
// This connects to your existing database with clean table names
//builder.Services.AddDbContext<InventoryDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with your custom models (database-first with clean table names)
builder.Services.AddIdentity<User, Role>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
//.AddEntityFrameworkStores<InventoryDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Optional: Configure session if needed
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Optional: Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("ManagerOnly", policy =>
        policy.RequireRole("Admin", "Manager"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Show detailed errors in development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Order matters - Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Optional: Use session if you added it
app.UseSession();

// Map default controller route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

// Seed initial data - Create admin user and roles if they don't exist
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Seed Roles
        string[] roleNames = { "Admin", "Manager", "Staff" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new Role
                {
                    Name = roleName,
                    Description = roleName == "Admin" ? "Full system access" :
                                  roleName == "Manager" ? "Can manage inventory and orders" :
                                  "Can view and process orders"
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation($"Created role: {roleName}");
                }
                else
                {
                    logger.LogError($"Failed to create role {roleName}: {string.Join(", ", result.Errors)}");
                }
            }
        }

        // Seed Admin User
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = "admin@inventory.com",
                FirstName = "System",
                LastName = "Administrator",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true,  // Admin doesn't need email confirmation
                LockoutEnabled = false   // Admin shouldn't be locked out
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Created admin user with Admin role");
            }
            else
            {
                logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors)}");
            }
        }

        // Seed a demo manager user (optional)
        var managerUser = await userManager.FindByNameAsync("manager");
        if (managerUser == null)
        {
            managerUser = new User
            {
                UserName = "manager",
                Email = "manager@inventory.com",
                FirstName = "Demo",
                LastName = "Manager",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(managerUser, "Manager@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
                logger.LogInformation("Created demo manager user");
            }
        }

        // Seed a demo staff user (optional)
        var staffUser = await userManager.FindByNameAsync("staff");
        if (staffUser == null)
        {
            staffUser = new User
            {
                UserName = "staff",
                Email = "staff@inventory.com",
                FirstName = "Demo",
                LastName = "Staff",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(staffUser, "Staff@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
                logger.LogInformation("Created demo staff user");
            }
        }

        // Check if database has any products (optional - for empty database)
        //var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        //if (!dbContext.Products.Any())
        //{
        //    logger.LogInformation("Database has no products. Consider adding sample data.");
        //}
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();