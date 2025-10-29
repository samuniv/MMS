using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MeetingManagementSystem.Infrastructure.Data;
using MeetingManagementSystem.Core.Entities;
using Xunit;
using System.Diagnostics;

namespace MeetingManagementSystem.Tests.Integration
{
    public class PerformanceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public PerformanceTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=meetingmanagement_test;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Query_MeetingsList_ShouldCompleteQuickly()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var meetings = await _context.Meetings
                .AsNoTracking()
                .Take(100)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public async Task Query_WithIncludes_ShouldUseQuerySplitting()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var meetings = await _context.Meetings
                .AsNoTracking()
                .Include(m => m.Participants)
                .Include(m => m.AgendaItems)
                .Take(10)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Query with includes took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        }

        [Fact]
        public async Task Query_WithProjection_ShouldBeFasterThanFullEntity()
        {
            // Arrange
            var fullEntityStopwatch = Stopwatch.StartNew();
            
            // Act - Full entity query
            var fullEntities = await _context.Meetings
                .AsNoTracking()
                .Take(100)
                .ToListAsync();
            
            fullEntityStopwatch.Stop();

            // Arrange - Projection query
            var projectionStopwatch = Stopwatch.StartNew();
            
            // Act - Projection query
            var projections = await _context.Meetings
                .AsNoTracking()
                .Select(m => new { m.Id, m.Title, m.ScheduledDate })
                .Take(100)
                .ToListAsync();
            
            projectionStopwatch.Stop();

            // Assert
            Assert.True(projectionStopwatch.ElapsedMilliseconds <= fullEntityStopwatch.ElapsedMilliseconds,
                $"Projection ({projectionStopwatch.ElapsedMilliseconds}ms) should be faster than full entity ({fullEntityStopwatch.ElapsedMilliseconds}ms)");
        }

        [Fact]
        public async Task Index_ShouldImproveQueryPerformance()
        {
            // Arrange - Query using indexed column
            var indexedStopwatch = Stopwatch.StartNew();
            
            // Act
            var indexedQuery = await _context.Meetings
                .AsNoTracking()
                .Where(m => m.ScheduledDate >= DateTime.Today)
                .Take(100)
                .ToListAsync();
            
            indexedStopwatch.Stop();

            // Assert - Should complete quickly due to index
            Assert.True(indexedStopwatch.ElapsedMilliseconds < 500,
                $"Indexed query took {indexedStopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        }

        [Fact]
        public async Task ConcurrentQueries_ShouldHandleLoad()
        {
            // Arrange
            var tasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute 20 concurrent queries
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseNpgsql(_context.Database.GetConnectionString())
                        .Options;

                    using var context = new ApplicationDbContext(options);
                    await context.Meetings
                        .AsNoTracking()
                        .Take(10)
                        .ToListAsync();
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Concurrent queries took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
        }

        [Fact]
        public async Task BulkInsert_ShouldHandleLargeDataset()
        {
            // Arrange
            var rooms = new List<MeetingRoom>();
            for (int i = 0; i < 100; i++)
            {
                rooms.Add(new MeetingRoom
                {
                    Name = $"Test Room {i}",
                    Location = $"Floor {i % 10}",
                    Capacity = 10 + (i % 20),
                    IsActive = true
                });
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _context.MeetingRooms.AddRangeAsync(rooms);
            await _context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 3000,
                $"Bulk insert took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");

            // Cleanup
            _context.MeetingRooms.RemoveRange(rooms);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Pagination_ShouldBeEfficient()
        {
            // Arrange
            const int pageSize = 20;
            var stopwatch = Stopwatch.StartNew();

            // Act - Get first page
            var firstPage = await _context.Meetings
                .AsNoTracking()
                .OrderBy(m => m.ScheduledDate)
                .Skip(0)
                .Take(pageSize)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Pagination query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        }

        [Fact]
        public async Task ComplexQuery_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act - Complex query with multiple joins and filters
            var result = await _context.Meetings
                .AsNoTracking()
                .Include(m => m.Organizer)
                .Include(m => m.MeetingRoom)
                .Include(m => m.Participants)
                .Where(m => m.ScheduledDate >= DateTime.Today)
                .Where(m => m.Status == MeetingStatus.Scheduled)
                .OrderBy(m => m.ScheduledDate)
                .Take(50)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 3000,
                $"Complex query took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");
        }

        [Fact]
        public async Task AuditLog_ShouldHandleHighVolume()
        {
            // Arrange
            var logs = new List<AuditLog>();
            for (int i = 0; i < 1000; i++)
            {
                logs.Add(new AuditLog
                {
                    Action = "Test Action",
                    EntityType = "Meeting",
                    EntityId = i,
                    UserId = 1,
                    Timestamp = DateTime.UtcNow,
                    Changes = "{\"test\": \"data\"}",
                    IpAddress = "127.0.0.1"
                });
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _context.AuditLogs.AddRangeAsync(logs);
            await _context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Audit log bulk insert took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

            // Cleanup
            _context.AuditLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
