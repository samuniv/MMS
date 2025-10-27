using Microsoft.Extensions.Logging;
using Moq;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IMeetingRoomRepository> _roomRepositoryMock;
    private readonly Mock<IMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<ILogger<RoomService>> _loggerMock;
    private readonly RoomService _roomService;

    public RoomServiceTests()
    {
        _roomRepositoryMock = new Mock<IMeetingRoomRepository>();
        _meetingRepositoryMock = new Mock<IMeetingRepository>();
        _loggerMock = new Mock<ILogger<RoomService>>();

        _roomService = new RoomService(
            _roomRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WithNoConflicts_ReturnsTrue()
    {
        // Arrange
        var roomId = 1;
        var date = DateTime.Today.AddDays(1);
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(11, 0, 0);

        _meetingRepositoryMock.Setup(r => r.HasRoomConflictAsync(
            roomId, date, startTime, endTime, null))
            .ReturnsAsync(false);

        // Act
        var result = await _roomService.IsRoomAvailableAsync(roomId, date, startTime, endTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WithConflict_ReturnsFalse()
    {
        // Arrange
        var roomId = 1;
        var date = DateTime.Today.AddDays(1);
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(11, 0, 0);

        _meetingRepositoryMock.Setup(r => r.HasRoomConflictAsync(
            roomId, date, startTime, endTime, null))
            .ReturnsAsync(true);

        // Act
        var result = await _roomService.IsRoomAvailableAsync(roomId, date, startTime, endTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_ReturnsOnlyAvailableRooms()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(11, 0, 0);

        var availableRooms = new List<MeetingRoom>
        {
            new MeetingRoom { Id = 1, Name = "Room 1", IsActive = true },
            new MeetingRoom { Id = 2, Name = "Room 2", IsActive = true }
        };

        _roomRepositoryMock.Setup(r => r.GetAvailableRoomsAsync(date, startTime, endTime))
            .ReturnsAsync(availableRooms);

        // Act
        var result = await _roomService.GetAvailableRoomsAsync(date, startTime, endTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAlternativeTimeSlotsAsync_WithBookings_ReturnsAlternatives()
    {
        // Arrange
        var roomId = 1;
        var date = DateTime.Today.AddDays(1);
        var desiredStartTime = new TimeSpan(10, 0, 0);
        var desiredEndTime = new TimeSpan(11, 0, 0);

        var existingBookings = new List<Meeting>
        {
            new Meeting 
            { 
                Id = 1, 
                MeetingRoomId = roomId,
                ScheduledDate = date,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                Status = MeetingStatus.Scheduled
            },
            new Meeting 
            { 
                Id = 2, 
                MeetingRoomId = roomId,
                ScheduledDate = date,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                Status = MeetingStatus.Scheduled
            }
        };

        _meetingRepositoryMock.Setup(r => r.GetMeetingsByRoomAsync(roomId, date))
            .ReturnsAsync(existingBookings);

        // Act
        var result = await _roomService.GetAlternativeTimeSlotsAsync(roomId, date, desiredStartTime, desiredEndTime);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRoomBookingsAsync_ReturnsBookingsForRoom()
    {
        // Arrange
        var roomId = 1;
        var date = DateTime.Today.AddDays(1);

        var bookings = new List<Meeting>
        {
            new Meeting { Id = 1, MeetingRoomId = roomId, ScheduledDate = date },
            new Meeting { Id = 2, MeetingRoomId = roomId, ScheduledDate = date }
        };

        _meetingRepositoryMock.Setup(r => r.GetMeetingsByRoomAsync(roomId, date))
            .ReturnsAsync(bookings);

        // Act
        var result = await _roomService.GetRoomBookingsAsync(roomId, date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}
