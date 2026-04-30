using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
// ML Services
builder.Services.AddScoped<MLDataService>();
builder.Services.AddScoped<ModelTrainerService>();
builder.Services.AddScoped<SampleDataGenerator>();
builder.Services.AddSingleton<ForecastService>();
builder.Services.AddScoped<Store_Management_System.Services.IChatbotService, Store_Management_System.Services.ChatbotService>();
// 1. Register the scaffolded business DbContext (unchanged)
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register the Identity DbContext (uses the same connection string)
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configure Identity to use ApplicationUser, ApplicationRole, and AppIdentityDbContext
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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
.AddEntityFrameworkStores<AppIdentityDbContext>()   // Use the Identity context
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

// Optional: Session and authorization policies
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Admin", "Manager"));
});
// Add ML Services
// ML Services
// ML Services
builder.Services.AddScoped<MLDataService>();
builder.Services.AddScoped<ModelTrainerService>();
builder.Services.AddScoped<SampleDataGenerator>();
builder.Services.AddSingleton<ForecastService>();
builder.Services.AddScoped<IntentModelTrainer>();  // ← ADD THIS LINE
builder.Services.AddScoped<Store_Management_System.Services.IChatbotService, Store_Management_System.Services.ChatbotService>();
// Add background service for weekly retraining
builder.Services.AddHostedService<ModelRetrainingBackgroundService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActivityLogService>();

var app = builder.Build();

// Test database connection (using InventoryDbContext)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Console.WriteLine("✅ Business database connection successful.");
            var articleCount = await dbContext.Articles.CountAsync();
            Console.WriteLine($"   Articles count: {articleCount}");
        }
        else
        {
            Console.WriteLine("❌ Business database connection failed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");
using (var scope = app.Services.CreateScope())
{
    var trainer = scope.ServiceProvider.GetRequiredService<IntentModelTrainer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking intent classification model...");
        trainer.TrainIfNotExists();
        logger.LogInformation("Intent model check complete.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to train intent model on startup");
        // Don't crash the app—the chatbot will fall back gracefully
    }
}
// Seed initial data (roles and users) using Identity (ApplicationUser, ApplicationRole)
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Seed Roles
        string[] roleNames = { "Admin", "Manager", "Staff" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName };
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                    logger.LogInformation($"Created role: {roleName}");
                else
                    logger.LogError($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
     
        // Helper to create a user
        async Task CreateUserIfNotExists(string userName, string email, string password, string firstName, string lastName, string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true,
                    LockoutEnabled = false
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation($"Created user '{userName}' with role '{role}'");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError($"Failed to create user {userName}: {errors}");
                }
            }
        }

        // Create admin users
        await CreateUserIfNotExists("admin", "admin@inventory.com", "Admin@123", "System", "Administrator", "Admin");
        await CreateUserIfNotExists("admin2", "admin2@example.com", "Admin2@123", "Second", "Admin", "Admin");
        await CreateUserIfNotExists("manager", "manager@inventory.com", "Manager@123", "Demo", "Manager", "Manager");
        await CreateUserIfNotExists("staff", "staff@inventory.com", "Staff@123", "Demo", "Staff", "Staff");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();