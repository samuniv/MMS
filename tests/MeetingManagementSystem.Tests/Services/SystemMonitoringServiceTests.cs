using Microsoft.EntityFrameworkCore;
using Moq;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class SystemMonitoringServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly SystemMonitoringService _monitoringService;

    public SystemMonitoringServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _monitoringService = new SystemMonitoringService(_context, _auditLogRepositoryMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", UserName = "john@test.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", UserName = "jane@test.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new User { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob@test.com", UserName = "bob@test.com", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-60) }
        };

        var rooms = new List<MeetingRoom>
        {
            new MeetingRoom { Id = 1, Name = "Room A", IsActive = true },
            new MeetingRoom { Id = 2, Name = "Room B", IsActive = false }
        };

        var meetings = new List<Meeting>
        {
            new Meeting { Id = 1, Title = "Meeting 1", ScheduledDate = DateTime.Today.AddDays(1), Status = MeetingStatus.Scheduled, OrganizerId = 1, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Meeting { Id = 2, Title = "Meeting 2", ScheduledDate = DateTime.Today.AddDays(2), Status = MeetingStatus.Scheduled, OrganizerId = 1, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Meeting { Id = 3, Title = "Meeting 3", ScheduledDate = DateTime.Today.AddDays(-5), Status = MeetingStatus.Completed, OrganizerId = 2, CreatedAt = DateTime.UtcNow.AddDays(-10) }
        };

        var documents = new List<MeetingDocument>
        {
            new MeetingDocument { Id = 1, MeetingId = 1, FileName = "doc1.pdf", FileSize = 1024 * 1024, UploadedAt = DateTime.UtcNow.AddDays(-3), UploadedById = 1 },
            new MeetingDocument { Id = 2, MeetingId = 2, FileName = "doc2.pdf", FileSize = 2 * 1024 * 1024, UploadedAt = DateTime.UtcNow.AddDays(-2), UploadedById = 2 }
        };

        var actionItems = new List<ActionItem>
        {
            new ActionItem { Id = 1, Description = "Task 1", Status = ActionItemStatus.Pending, DueDate = DateTime.Today.AddDays(5), CreatedAt = DateTime.UtcNow.AddDays(-3), AssignedToId = 1 },
            new ActionItem { Id = 2, Description = "Task 2", Status = ActionItemStatus.InProgress, DueDate = DateTime.Today.AddDays(3), CreatedAt = DateTime.UtcNow.AddDays(-2), AssignedToId = 2 },
            new ActionItem { Id = 3, Description = "Task 3", Status = ActionItemStatus.Pending, DueDate = DateTime.Today.AddDays(-2), CreatedAt = DateTime.UtcNow.AddDays(-10), AssignedToId = 1 },
            new ActionItem { Id = 4, Description = "Task 4", Status = ActionItemStatus.Completed, DueDate = DateTime.Today.AddDays(-5), CreatedAt = DateTime.UtcNow.AddDays(-15), AssignedToId = 2 }
        };

        _context.Users.AddRange(users);
        _context.MeetingRooms.AddRange(rooms);
        _context.Meetings.AddRange(meetings);
        _context.MeetingDocuments.AddRange(documents);
        _context.ActionItems.AddRange(actionItems);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ReturnsCorrectMetrics()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalUsers);
        Assert.Equal(2, result.ActiveUsers);
        Assert.Equal(3, result.TotalMeetings);
        Assert.Equal(2, result.UpcomingMeetings);
        Assert.Equal(2, result.TotalMeetingRooms);
        Assert.Equal(1, result.ActiveRooms);
        Assert.Equal(2, result.TotalDocuments);
        Assert.Equal(3, result.TotalDocumentSizeMB); // 1MB + 2MB
        Assert.Equal(3, result.PendingActionItems); // 2 Pending + 1 InProgress
        Assert.Equal(1, result.OverdueActionItems);
    }

    [Fact]
    public async Task GetSystemHealthAsync_SetsWarningStatusWhenManyOverdueItems()
    {
        // Arrange - Add more overdue items
        for (int i = 5; i <= 15; i++)
        {
            _context.ActionItems.Add(new ActionItem
            {
                Id = i,
                Description = $"Task {i}",
                Status = ActionItemStatus.Pending,
                DueDate = DateTime.Today.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                AssignedToId = 1
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _monitoringService.GetSystemHealthAsync();

        // Assert
        Assert.Equal("Warning", result.SystemStatus);
        Assert.True(result.OverdueActionItems > 10);
    }

    [Fact]
    public async Task GetSystemHealthAsync_IncludesRecentActivity()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync();

        // Assert
        Assert.NotNull(result.RecentActivity);
        Assert.True(result.RecentActivity.ContainsKey("Meetings Created"));
        Assert.True(result.RecentActivity.ContainsKey("Documents Uploaded"));
        Assert.True(result.RecentActivity.ContainsKey("Action Items Created"));
        Assert.True(result.RecentActivity.ContainsKey("Users Registered"));
    }

    [Fact]
    public async Task GetRecentAuditLogsAsync_ReturnsLogsFromRepository()
    {
        // Arrange
        var mockLogs = new List<AuditLog>
        {
            new AuditLog { Id = 1, Action = "Create", EntityType = "Meeting", EntityId = 1, UserId = 1, Timestamp = DateTime.UtcNow, User = new User { FirstName = "John", LastName = "Doe" } },
            new AuditLog { Id = 2, Action = "Update", EntityType = "Meeting", EntityId = 1, UserId = 2, Timestamp = DateTime.UtcNow, User = new User { FirstName = "Jane", LastName = "Smith" } }
        };

        _auditLogRepositoryMock.Setup(r => r.GetRecentLogsAsync(It.IsAny<int>()))
            .ReturnsAsync(mockLogs);

        // Act
        var result = await _monitoringService.GetRecentAuditLogsAsync(10);

        // Assert
        Assert.NotNull(result);
        var logList = result.ToList();
        Assert.Equal(2, logList.Count);
        Assert.Equal("Create", logList[0].Action);
        Assert.Equal("John Doe", logList[0].UserName);
    }

    [Fact]
    public async Task GetAuditLogsByUserAsync_ReturnsFilteredLogs()
    {
        // Arrange
        var userId = 1;
        var mockLogs = new List<AuditLog>
        {
            new AuditLog { Id = 1, Action = "Create", EntityType = "Meeting", EntityId = 1, UserId = userId, Timestamp = DateTime.UtcNow, User = new User { FirstName = "John", LastName = "Doe" } }
        };

        _auditLogRepositoryMock.Setup(r => r.GetLogsByUserAsync(userId, null, null))
            .ReturnsAsync(mockLogs);

        // Act
        var result = await _monitoringService.GetAuditLogsByUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("John Doe", result.First().UserName);
    }

    [Fact]
    public async Task GetAuditLogsByDateRangeAsync_ReturnsFilteredLogs()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;
        var mockLogs = new List<AuditLog>
        {
            new AuditLog { Id = 1, Action = "Create", EntityType = "Meeting", EntityId = 1, UserId = 1, Timestamp = DateTime.UtcNow.AddDays(-3), User = new User { FirstName = "John", LastName = "Doe" } }
        };

        _auditLogRepositoryMock.Setup(r => r.GetLogsByDateRangeAsync(startDate, endDate))
            .ReturnsAsync(mockLogs);

        // Act
        var result = await _monitoringService.GetAuditLogsByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task LogUserActionAsync_CallsRepository()
    {
        // Arrange
        var action = "Create";
        var entityType = "Meeting";
        var entityId = 1;
        var userId = 1;
        var changes = "Created new meeting";
        var ipAddress = "127.0.0.1";

        _auditLogRepositoryMock.Setup(r => r.LogActionAsync(action, entityType, entityId, userId, changes, ipAddress))
            .Returns(Task.CompletedTask);

        // Act
        await _monitoringService.LogUserActionAsync(action, entityType, entityId, userId, changes, ipAddress);

        // Assert
        _auditLogRepositoryMock.Verify(r => r.LogActionAsync(action, entityType, entityId, userId, changes, ipAddress), Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
