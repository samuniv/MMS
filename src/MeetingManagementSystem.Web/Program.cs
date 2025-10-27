using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Constants;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/meetingmanagement-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();

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

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

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