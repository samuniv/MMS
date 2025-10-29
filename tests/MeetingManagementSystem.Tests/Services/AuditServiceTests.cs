using Moq;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _auditService = new AuditService(_auditLogRepositoryMock.Object);
    }

    [Fact]
    public async Task LogCreateAsync_WithValidEntity_LogsSuccessfully()
    {
        // Arrange
        var meeting = new Meeting
        {
            Id = 1,
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1
        };
        var userId = 1;
        var ipAddress = "192.168.1.1";

        _auditLogRepositoryMock
            .Setup(r => r.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _auditService.LogCreateAsync(meeting, userId, ipAddress);

        // Assert
        _auditLogRepositoryMock.Verify(r => r.LogActionAsync(
            "Create",
            "Meeting",
            1,
            userId,
            It.IsAny<string>(),
            ipAddress
        ), Times.Once);
    }

    [Fact]
    public async Task LogUpdateAsync_WithChangedEntity_LogsChanges()
    {
        // Arrange
        var oldMeeting = new Meeting
        {
            Id = 1,
            Title = "Old Title",
            Description = "Old Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1
        };

        var newMeeting = new Meeting
        {
            Id = 1,
            Title = "New Title",
            Description = "New Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1
        };

        var userId = 1;
        var ipAddress = "192.168.1.1";

        _auditLogRepositoryMock
            .Setup(r => r.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _auditService.LogUpdateAsync(oldMeeting, newMeeting, userId, ipAddress);

        // Assert
        _auditLogRepositoryMock.Verify(r => r.LogActionAsync(
            "Update",
            "Meeting",
            1,
            userId,
            It.Is<string>(s => s.Contains("Title") && s.Contains("Description")),
            ipAddress
        ), Times.Once);
    }

    [Fact]
    public async Task LogDeleteAsync_WithValidEntity_LogsSuccessfully()
    {
        // Arrange
        var meeting = new Meeting
        {
            Id = 1,
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1
        };
        var userId = 1;
        var ipAddress = "192.168.1.1";

        _auditLogRepositoryMock
            .Setup(r => r.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _auditService.LogDeleteAsync(meeting, userId, ipAddress);

        // Assert
        _auditLogRepositoryMock.Verify(r => r.LogActionAsync(
            "Delete",
            "Meeting",
            1,
            userId,
            It.IsAny<string>(),
            ipAddress
        ), Times.Once);
    }

    [Fact]
    public async Task GetFilteredLogsAsync_WithFilters_ReturnsFilteredLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = 1,
                Action = "Create",
                EntityType = "Meeting",
                EntityId = 1,
                UserId = 1,
                Timestamp = DateTime.UtcNow,
                Changes = "{}",
                IpAddress = "192.168.1.1"
            },
            new AuditLog
            {
                Id = 2,
                Action = "Update",
                EntityType = "Meeting",
                EntityId = 1,
                UserId = 1,
                Timestamp = DateTime.UtcNow,
                Changes = "{}",
                IpAddress = "192.168.1.1"
            }
        };

        _auditLogRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        // Act
        var result = await _auditService.GetFilteredLogsAsync(entityType: "Meeting", action: "Create");

        // Assert
        Assert.Single(result);
        Assert.Equal("Create", result.First().Action);
    }
}
