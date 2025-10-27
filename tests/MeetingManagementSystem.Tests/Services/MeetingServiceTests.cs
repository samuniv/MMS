using Microsoft.Extensions.Logging;
using Moq;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Exceptions;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class MeetingServiceTests
{
    private readonly Mock<IMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IMeetingParticipantRepository> _participantRepositoryMock;
    private readonly Mock<IMeetingRoomRepository> _roomRepositoryMock;
    private readonly Mock<ILogger<MeetingService>> _loggerMock;
    private readonly MeetingService _meetingService;

    public MeetingServiceTests()
    {
        _meetingRepositoryMock = new Mock<IMeetingRepository>();
        _participantRepositoryMock = new Mock<IMeetingParticipantRepository>();
        _roomRepositoryMock = new Mock<IMeetingRoomRepository>();
        _loggerMock = new Mock<ILogger<MeetingService>>();

        _meetingService = new MeetingService(
            _meetingRepositoryMock.Object,
            _participantRepositoryMock.Object,
            _roomRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateMeetingAsync_WithValidData_CreatesMeeting()
    {
        // Arrange
        var dto = new CreateMeetingDto
        {
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1,
            MeetingRoomId = 1,
            ParticipantIds = new List<int> { 2, 3 }
        };

        _meetingRepositoryMock.Setup(r => r.HasRoomConflictAsync(
            It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        _meetingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Meeting>()))
            .ReturnsAsync((Meeting m) => { m.Id = 1; return m; });

        _participantRepositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<MeetingParticipant>>()))
            .ReturnsAsync((IEnumerable<MeetingParticipant> p) => p);

        // Act
        var result = await _meetingService.CreateMeetingAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(MeetingStatus.Scheduled, result.Status);
        _meetingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Meeting>()), Times.Once);
        _participantRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<MeetingParticipant>>()), Times.Once);
    }

    [Fact]
    public async Task CreateMeetingAsync_WithRoomConflict_ThrowsException()
    {
        // Arrange
        var dto = new CreateMeetingDto
        {
            Title = "Test Meeting",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            OrganizerId = 1,
            MeetingRoomId = 1
        };

        _meetingRepositoryMock.Setup(r => r.HasRoomConflictAsync(
            It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<RoomNotAvailableException>(() => _meetingService.CreateMeetingAsync(dto));
        _meetingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Meeting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMeetingStatusAsync_WithValidId_UpdatesStatus()
    {
        // Arrange
        var meetingId = 1;
        var newStatus = MeetingStatus.InProgress;
        var meeting = new Meeting { Id = meetingId, Status = MeetingStatus.Scheduled };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId))
            .ReturnsAsync(meeting);

        _meetingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Meeting>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _meetingService.UpdateMeetingStatusAsync(meetingId, newStatus);

        // Assert
        Assert.True(result);
        Assert.Equal(newStatus, meeting.Status);
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Meeting>()), Times.Once);
    }

    [Fact]
    public async Task CancelMeetingAsync_WithValidId_CancelsMeeting()
    {
        // Arrange
        var meetingId = 1;
        var meeting = new Meeting { Id = meetingId, Status = MeetingStatus.Scheduled };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId))
            .ReturnsAsync(meeting);

        _meetingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Meeting>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _meetingService.CancelMeetingAsync(meetingId, "Test reason");

        // Assert
        Assert.True(result);
        Assert.Equal(MeetingStatus.Cancelled, meeting.Status);
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Meeting>()), Times.Once);
    }

    [Fact]
    public async Task AddParticipantAsync_WithNewParticipant_AddsSuccessfully()
    {
        // Arrange
        var meetingId = 1;
        var userId = 2;
        var meeting = new Meeting { Id = meetingId };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId))
            .ReturnsAsync(meeting);

        _participantRepositoryMock.Setup(r => r.GetParticipantAsync(meetingId, userId))
            .ReturnsAsync((MeetingParticipant?)null);

        _participantRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingParticipant>()))
            .ReturnsAsync((MeetingParticipant p) => p);

        // Act
        var result = await _meetingService.AddParticipantAsync(meetingId, userId);

        // Assert
        Assert.True(result);
        _participantRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MeetingParticipant>()), Times.Once);
    }

    [Fact]
    public async Task UpdateParticipantStatusAsync_WithValidData_UpdatesStatus()
    {
        // Arrange
        var meetingId = 1;
        var userId = 2;
        var newStatus = AttendanceStatus.Accepted;
        var participant = new MeetingParticipant 
        { 
            MeetingId = meetingId, 
            UserId = userId, 
            AttendanceStatus = AttendanceStatus.Pending 
        };

        _participantRepositoryMock.Setup(r => r.GetParticipantAsync(meetingId, userId))
            .ReturnsAsync(participant);

        _participantRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MeetingParticipant>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _meetingService.UpdateParticipantStatusAsync(meetingId, userId, newStatus);

        // Assert
        Assert.True(result);
        Assert.Equal(newStatus, participant.AttendanceStatus);
        Assert.NotNull(participant.ResponseDate);
        _participantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MeetingParticipant>()), Times.Once);
    }
}
