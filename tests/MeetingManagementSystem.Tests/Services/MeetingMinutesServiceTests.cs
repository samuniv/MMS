using Microsoft.Extensions.Logging;
using Moq;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class MeetingMinutesServiceTests
{
    private readonly Mock<IMeetingMinutesRepository> _minutesRepositoryMock;
    private readonly Mock<IMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<ILogger<MeetingMinutesService>> _loggerMock;
    private readonly MeetingMinutesService _minutesService;

    public MeetingMinutesServiceTests()
    {
        _minutesRepositoryMock = new Mock<IMeetingMinutesRepository>();
        _meetingRepositoryMock = new Mock<IMeetingRepository>();
        _loggerMock = new Mock<ILogger<MeetingMinutesService>>();

        _minutesService = new MeetingMinutesService(
            _minutesRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateMinutesAsync_WithValidData_CreatesMinutes()
    {
        // Arrange
        var dto = new CreateMeetingMinutesDto
        {
            MeetingId = 1,
            Content = "Test meeting minutes content",
            CreatedById = 1
        };

        var meeting = new Meeting { Id = 1, Title = "Test Meeting" };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(dto.MeetingId))
            .ReturnsAsync(meeting);

        _minutesRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingMinutes>()))
            .ReturnsAsync((MeetingMinutes m) => { m.Id = 1; return m; });

        // Act
        var result = await _minutesService.CreateMinutesAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.MeetingId, result.MeetingId);
        Assert.Equal(dto.Content, result.Content);
        Assert.Equal(dto.CreatedById, result.CreatedById);
        _minutesRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MeetingMinutes>()), Times.Once);
    }

    [Fact]
    public async Task CreateMinutesAsync_WithInvalidMeetingId_ThrowsException()
    {
        // Arrange
        var dto = new CreateMeetingMinutesDto
        {
            MeetingId = 999,
            Content = "Test content",
            CreatedById = 1
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(dto.MeetingId))
            .ReturnsAsync((Meeting?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _minutesService.CreateMinutesAsync(dto));
    }

    [Fact]
    public async Task UpdateMinutesAsync_WithValidData_UpdatesMinutes()
    {
        // Arrange
        var minutesId = 1;
        var dto = new UpdateMeetingMinutesDto
        {
            Content = "Updated content"
        };

        var originalLastModified = DateTime.UtcNow.AddDays(-1);
        var existingMinutes = new MeetingMinutes
        {
            Id = minutesId,
            MeetingId = 1,
            Content = "Original content",
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = originalLastModified
        };

        _minutesRepositoryMock.Setup(r => r.GetByIdAsync(minutesId))
            .ReturnsAsync(existingMinutes);

        _minutesRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MeetingMinutes>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _minutesService.UpdateMinutesAsync(minutesId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Content, result.Content);
        Assert.True(result.LastModified >= originalLastModified);
        _minutesRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MeetingMinutes>()), Times.Once);
    }

    [Fact]
    public async Task GetMinutesByMeetingIdAsync_ReturnsMinutes()
    {
        // Arrange
        var meetingId = 1;
        var minutes = new MeetingMinutes
        {
            Id = 1,
            MeetingId = meetingId,
            Content = "Test content",
            CreatedById = 1
        };

        _minutesRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meetingId))
            .ReturnsAsync(minutes);

        // Act
        var result = await _minutesService.GetMinutesByMeetingIdAsync(meetingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(meetingId, result.MeetingId);
        Assert.Equal(minutes.Content, result.Content);
    }

    [Fact]
    public async Task DeleteMinutesAsync_WithValidId_DeletesMinutes()
    {
        // Arrange
        var minutesId = 1;
        var minutes = new MeetingMinutes
        {
            Id = minutesId,
            MeetingId = 1,
            Content = "Test content",
            CreatedById = 1
        };

        _minutesRepositoryMock.Setup(r => r.GetByIdAsync(minutesId))
            .ReturnsAsync(minutes);

        _minutesRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<MeetingMinutes>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _minutesService.DeleteMinutesAsync(minutesId);

        // Assert
        Assert.True(result);
        _minutesRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<MeetingMinutes>()), Times.Once);
    }
}
