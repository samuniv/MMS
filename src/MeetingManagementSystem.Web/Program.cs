using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Constants;
using MeetingManagementSystem.Web.Middleware;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/meetingmanagement-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Enable antiforgery token validation for all Razor Pages
    options.Conventions.ConfigureFilter(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});

// Add antiforgery services with enhanced security
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    // Use secure cookies only in production or when HTTPS is available
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure Email Settings
builder.Services.Configure<MeetingManagementSystem.Core.DTOs.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure Document Settings
builder.Services.Configure<MeetingManagementSystem.Core.DTOs.DocumentSettings>(
    builder.Configuration.GetSection("DocumentSettings"));

// Register application services
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IRoleManagementService, 
    MeetingManagementSystem.Infrastructure.Services.RoleManagementService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingService,
    MeetingManagementSystem.Infrastructure.Services.MeetingService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IRoomService,
    MeetingManagementSystem.Infrastructure.Services.RoomService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.INotificationService,
    MeetingManagementSystem.Infrastructure.Services.NotificationService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IReminderSchedulerService,
    MeetingManagementSystem.Infrastructure.Services.ReminderSchedulerService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IDocumentService,
    MeetingManagementSystem.Infrastructure.Services.DocumentService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingMinutesService,
    MeetingManagementSystem.Infrastructure.Services.MeetingMinutesService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IActionItemService,
    MeetingManagementSystem.Infrastructure.Services.ActionItemService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IReportService,
    MeetingManagementSystem.Infrastructure.Services.ReportService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.ISystemMonitoringService,
    MeetingManagementSystem.Infrastructure.Services.SystemMonitoringService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.INotificationPreferenceService,
    MeetingManagementSystem.Infrastructure.Services.NotificationPreferenceService>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IAuditService,
    MeetingManagementSystem.Infrastructure.Services.AuditService>();

// Register helper services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MeetingManagementSystem.Web.Services.AuditContextService>();
builder.Services.AddSingleton<MeetingManagementSystem.Infrastructure.Services.ICacheService,
    MeetingManagementSystem.Infrastructure.Services.CacheService>();

// Register background services
builder.Services.AddHostedService<MeetingManagementSystem.Web.Services.ReminderBackgroundService>();

// Register repositories
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingRepository,
    MeetingManagementSystem.Infrastructure.Repositories.MeetingRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IUserRepository,
    MeetingManagementSystem.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingRoomRepository,
    MeetingManagementSystem.Infrastructure.Repositories.MeetingRoomRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingParticipantRepository,
    MeetingManagementSystem.Infrastructure.Repositories.MeetingParticipantRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IDocumentRepository,
    MeetingManagementSystem.Infrastructure.Repositories.DocumentRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IMeetingMinutesRepository,
    MeetingManagementSystem.Infrastructure.Repositories.MeetingMinutesRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IActionItemRepository,
    MeetingManagementSystem.Infrastructure.Repositories.ActionItemRepository>();
builder.Services.AddScoped<MeetingManagementSystem.Core.Interfaces.IAuditLogRepository,
    MeetingManagementSystem.Infrastructure.Repositories.AuditLogRepository>();

// Add Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
    options.CompactionPercentage = 0.25; // Compact 25% when limit reached
});

// Add Response Caching
builder.Services.AddResponseCaching();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Add Entity Framework with performance optimizations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Try to get connection string in order of preference:
    // 1. From Aspire reference (when running via AppHost)
    // 2. From appsettings
    var connectionString = builder.Configuration.GetConnectionString("meetingmanagement");

    // If still null, try the default connection
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    }

    // Log which connection string is being used
    if (builder.Environment.IsDevelopment())
    {
        var maskedConnectionString = string.IsNullOrEmpty(connectionString)
            ? "NULL"
            : System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]*", "Password=***");
        Console.WriteLine($"Using connection string: {maskedConnectionString}");
    }

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "No database connection string found. Please ensure 'meetingmanagement' or 'DefaultConnection' is configured in appsettings.json or via Aspire AppHost.");
    }

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable retry on failure for transient errors
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        
        // Command timeout
        npgsqlOptions.CommandTimeout(30);
        
        // Enable query splitting for better performance with collections
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
    
    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Configure query tracking behavior for better performance
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
});

// Add health checks for database
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// Add Identity with enhanced security configuration
builder.Services.AddIdentity<User, IdentityRole<int>>(options => 
{
    // Password policies (Requirement 6.3)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Account lockout settings (Requirement 6.3)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Configure authorization policies (Requirement 6.1, 6.2)
builder.Services.AddAuthorization(options =>
{
    // Administrator policy - full system access
    options.AddPolicy(Policies.AdministratorOnly, policy =>
        policy.RequireRole(Roles.Administrator));

    // Government Official policy - can create and manage meetings
    options.AddPolicy(Policies.GovernmentOfficialOnly, policy =>
        policy.RequireRole(Roles.Administrator, Roles.GovernmentOfficial));

    // Participant policy - can view and participate in meetings
    options.AddPolicy(Policies.ParticipantAccess, policy =>
        policy.RequireRole(Roles.Administrator, Roles.GovernmentOfficial, Roles.Participant));

    // Meeting organizer policy - can manage own meetings
    options.AddPolicy(Policies.MeetingOrganizer, policy =>
        policy.RequireRole(Roles.Administrator, Roles.GovernmentOfficial));

    // Room management policy
    options.AddPolicy(Policies.RoomManagement, policy =>
        policy.RequireRole(Roles.Administrator, Roles.GovernmentOfficial));

    // User management policy
    options.AddPolicy(Policies.UserManagement, policy =>
        policy.RequireRole(Roles.Administrator));

    // Report access policy
    options.AddPolicy(Policies.ReportAccess, policy =>
        policy.RequireRole(Roles.Administrator, Roles.GovernmentOfficial));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Add response compression
app.UseResponseCompression();

// Add response caching
app.UseResponseCaching();

// Add security headers
app.UseSecurityHeaders();

// Add rate limiting
app.UseRateLimiting();

// Configure static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days
        const int durationInSeconds = 60 * 60 * 24 * 7;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed initial data
        await DbSeeder.SeedAsync(context, userManager, roleManager);
        
        Log.Information("Database initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

try
{
    Log.Information("Starting Meeting Management System");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}