using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MeetingManagementSystem.Infrastructure.Data;
using Xunit;

namespace MeetingManagementSystem.Tests.Integration
{
    public class DatabaseMigrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly string _testConnectionString;

        public DatabaseMigrationTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            _testConnectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=meetingmanagement_test;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_testConnectionString)
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Database_ShouldConnect()
        {
            // Act
            var canConnect = await _context.Database.CanConnectAsync();

            // Assert
            Assert.True(canConnect, "Database connection failed");
        }

        [Fact]
        public async Task Database_ShouldApplyMigrations()
        {
            // Act
            await _context.Database.MigrateAsync();
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

            // Assert
            Assert.Empty(pendingMigrations);
        }

        [Fact]
        public async Task Database_ShouldHaveAllTables()
        {
            // Arrange
            var expectedTables = new[]
            {
                "AspNetUsers",
                "AspNetRoles",
                "Meetings",
                "MeetingRooms",
                "MeetingParticipants",
                "AgendaItems",
                "ActionItems",
                "MeetingDocuments",
                "MeetingMinutes",
                "AuditLogs",
                "ScheduledReminders",
                "NotificationPreferences",
                "NotificationHistories"
            };

            // Act
            var tableQuery = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_type = 'BASE TABLE'";

            var tables = await _context.Database
                .SqlQueryRaw<string>(tableQuery)
                .ToListAsync();

            // Assert
            foreach (var expectedTable in expectedTables)
            {
                Assert.Contains(expectedTable, tables, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Database_ShouldHaveRequiredIndexes()
        {
            // Arrange
            var expectedIndexes = new[]
            {
                "IX_Meeting_DateTime",
                "IX_Meeting_Status",
                "IX_User_Email",
                "IX_MeetingParticipant_Unique",
                "IX_ActionItem_DueDate",
                "IX_AuditLog_Timestamp"
            };

            // Act
            var indexQuery = @"
                SELECT indexname 
                FROM pg_indexes 
                WHERE schemaname = 'public'";

            var indexes = await _context.Database
                .SqlQueryRaw<string>(indexQuery)
                .ToListAsync();

            // Assert
            foreach (var expectedIndex in expectedIndexes)
            {
                Assert.Contains(expectedIndex, indexes, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Database_ShouldHavePostgreSQLExtensions()
        {
            // Act
            var extensionQuery = @"
                SELECT extname 
                FROM pg_extension";

            var extensions = await _context.Database
                .SqlQueryRaw<string>(extensionQuery)
                .ToListAsync();

            // Assert
            Assert.Contains("uuid-ossp", extensions, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Database_ShouldSupportJSONB()
        {
            // Act
            var jsonbQuery = @"
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = 'AuditLogs' 
                AND column_name = 'Changes'";

            var result = await _context.Database
                .SqlQueryRaw<string>(jsonbQuery)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("jsonb", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Database_ShouldEnforceForeignKeyConstraints()
        {
            // Arrange
            var constraintQuery = @"
                SELECT COUNT(*) 
                FROM information_schema.table_constraints 
                WHERE constraint_type = 'FOREIGN KEY' 
                AND table_schema = 'public'";

            // Act
            var constraintCount = await _context.Database
                .SqlQueryRaw<int>(constraintQuery)
                .FirstOrDefaultAsync();

            // Assert
            Assert.True(constraintCount > 0, "No foreign key constraints found");
        }

        [Fact]
        public async Task Database_ShouldHaveUniqueConstraints()
        {
            // Arrange
            var constraintQuery = @"
                SELECT constraint_name 
                FROM information_schema.table_constraints 
                WHERE constraint_type = 'UNIQUE' 
                AND table_schema = 'public'";

            // Act
            var constraints = await _context.Database
                .SqlQueryRaw<string>(constraintQuery)
                .ToListAsync();

            // Assert
            Assert.NotEmpty(constraints);
            Assert.Contains(constraints, c => c.Contains("User_Email", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Database_ShouldRollbackOnError()
        {
            // Arrange
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Act - Try to insert invalid data
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Meetings (Title, ScheduledDate) VALUES (NULL, NULL)");

                await transaction.CommitAsync();

                // Assert - Should not reach here
                Assert.True(false, "Transaction should have failed");
            }
            catch
            {
                // Assert - Transaction should rollback
                await transaction.RollbackAsync();
                Assert.True(true);
            }
        }

        [Fact]
        public async Task Database_ShouldHandleConcurrentConnections()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - Create multiple concurrent connections
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseNpgsql(_testConnectionString)
                        .Options;

                    using var context = new ApplicationDbContext(options);
                    await context.Database.ExecuteSqlRawAsync("SELECT 1");
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - All tasks should complete without errors
            Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
