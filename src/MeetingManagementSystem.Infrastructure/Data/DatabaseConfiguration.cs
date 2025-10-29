using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MeetingManagementSystem.Infrastructure.Data
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddOptimizedDatabase(
            this IServiceCollection services, 
            IConfiguration configuration,
            bool isDevelopment = false)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configure Npgsql connection pooling
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            
            // Enable connection pooling with optimized settings
            dataSourceBuilder.ConnectionStringBuilder.Pooling = true;
            dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 5;
            dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
            dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 minutes
            dataSourceBuilder.ConnectionStringBuilder.ConnectionPruningInterval = 10;
            
            // Performance settings
            dataSourceBuilder.ConnectionStringBuilder.MaxAutoPrepare = 20;
            dataSourceBuilder.ConnectionStringBuilder.AutoPrepareMinUsages = 2;
            dataSourceBuilder.ConnectionStringBuilder.CommandTimeout = 30;
            dataSourceBuilder.ConnectionStringBuilder.Timeout = 15;
            dataSourceBuilder.ConnectionStringBuilder.KeepAlive = 30;
            
            // Build the data source
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(dataSource, npgsqlOptions =>
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
                    
                    // Migration settings
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
                
                // Enable sensitive data logging only in development
                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
                
                // Configure query tracking behavior for better performance
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                
                // Configure warnings
                options.ConfigureWarnings(warnings =>
                {
                    // Suppress specific warnings if needed
                    // warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                });
            });

            return services;
        }

        public static async Task OptimizeDatabaseAsync(ApplicationDbContext context)
        {
            // Run VACUUM ANALYZE for PostgreSQL optimization
            await context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE;");
            
            // Update statistics
            await context.Database.ExecuteSqlRawAsync("ANALYZE;");
        }

        public static async Task<DatabaseHealthInfo> CheckDatabaseHealthAsync(ApplicationDbContext context)
        {
            var healthInfo = new DatabaseHealthInfo();

            try
            {
                // Check connection
                healthInfo.CanConnect = await context.Database.CanConnectAsync();

                if (healthInfo.CanConnect)
                {
                    // Get database size
                    var sizeQuery = @"
                        SELECT pg_size_pretty(pg_database_size(current_database())) as size";
                    var sizeResult = await context.Database
                        .SqlQueryRaw<string>(sizeQuery)
                        .FirstOrDefaultAsync();
                    healthInfo.DatabaseSize = sizeResult ?? "Unknown";

                    // Get connection count
                    var connectionQuery = @"
                        SELECT count(*) 
                        FROM pg_stat_activity 
                        WHERE datname = current_database()";
                    healthInfo.ActiveConnections = await context.Database
                        .SqlQueryRaw<int>(connectionQuery)
                        .FirstOrDefaultAsync();

                    // Get table statistics
                    var tableStatsQuery = @"
                        SELECT 
                            schemaname,
                            tablename,
                            pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
                        FROM pg_tables
                        WHERE schemaname = 'public'
                        ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
                        LIMIT 10";
                    
                    healthInfo.IsHealthy = true;
                }
            }
            catch (Exception ex)
            {
                healthInfo.IsHealthy = false;
                healthInfo.ErrorMessage = ex.Message;
            }

            return healthInfo;
        }
    }

    public class DatabaseHealthInfo
    {
        public bool IsHealthy { get; set; }
        public bool CanConnect { get; set; }
        public string DatabaseSize { get; set; } = string.Empty;
        public int ActiveConnections { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
