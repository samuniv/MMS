using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MeetingManagementSystem.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace MeetingManagementSystem.E2ETests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that configures the application for E2E testing.
/// Uses a PostgreSQL Testcontainer and configures test-specific services.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
        
        // Set environment variable to tell ASP.NET Core where to find the Web project
        // This fixes the deps.json lookup issue when using WebApplicationFactory with Playwright
        var webProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "MeetingManagementSystem.Web");
        Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_MEETINGMANAGEMENTSYSTEM_WEB", Path.GetFullPath(webProjectPath));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddJsonFile("appsettings.Testing.json", optional: false);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ConnectionStrings:meetingmanagement"] = _connectionString
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                });

                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Initialize the database with migrations and seed data.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created and migrations are applied
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Reset the database to a clean state using Respawn.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var connection = context.Database.GetDbConnection();
        
        // Ensure connection is open
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        
        // Use Respawn to reset database
        var respawner = await Respawn.Respawner.CreateAsync(
            connection,
            new Respawn.RespawnerOptions
            {
                DbAdapter = Respawn.DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
            });

        await respawner.ResetAsync(connection);
    }
}
