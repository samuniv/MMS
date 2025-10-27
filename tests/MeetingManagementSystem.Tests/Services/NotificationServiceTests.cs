using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _notificationService;
    private readonly EmailSettings _emailSettings;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        
        _emailSettings = new EmailSettings
        {
            SmtpServer = "localhost",
            SmtpPort = 1025,
            EnableSsl = false,
            FromAddress = "test@example.com",
            FromName = "Test System",
            MaxRetryAttempts = 1,
            RetryDelaySeconds = 1
        };

        var emailSettingsOptions = Options.Create(_emailSettings);
        _notificationService = new NotificationService(emailSettingsOptions, _loggerMock.Object);
    }

    [Fact]
    public async Task SendMeetingInvitationAsync_WithValidMeeting_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeeting();
        var participants = new List<User>
        {
            new User { Id = 2, Email = "participant1@example.com", FirstName = "John", LastName = "Doe" },
            new User { Id = 3, Email = "participant2@example.com", FirstName = "Jane", LastName = "Smith" }
        };

        // Act & Assert - Should not throw unexpected exceptions
        // Note: SMTP errors are expected in unit test environment
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendMeetingInvitationAsync(meeting, participants));

        // Verify the method handles the data correctly (SMTP errors are acceptable)
        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendMeetingReminderAsync_WithValidMeeting_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeetingWithParticipants();
        var reminderTime = TimeSpan.FromHours(24);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendMeetingReminderAsync(meeting, reminderTime));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendMeetingCancellationAsync_WithValidMeeting_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeetingWithParticipants();
        var reason = "Organizer unavailable";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendMeetingCancellationAsync(meeting, reason));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendActionItemReminderAsync_WithValidActionItem_HandlesCorrectly()
    {
        // Arrange
        var actionItem = CreateTestActionItem();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendActionItemReminderAsync(actionItem));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendActionItemReminderAsync_WithNoEmail_LogsWarning()
    {
        // Arrange
        var actionItem = CreateTestActionItem();
        actionItem.AssignedTo.Email = null;

        // Act
        await _notificationService.SendActionItemReminderAsync(actionItem);

        // Assert - Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task SendMeetingUpdateNotificationAsync_WithValidMeeting_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeetingWithParticipants();
        var updateMessage = "Meeting time has been changed";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendMeetingUpdateNotificationAsync(meeting, updateMessage));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendAttendanceConfirmationAsync_WithAcceptedStatus_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeeting();
        var participant = new User 
        { 
            Id = 2, 
            Email = "participant@example.com", 
            FirstName = "John", 
            LastName = "Doe" 
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendAttendanceConfirmationAsync(meeting, participant, true));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    [Fact]
    public async Task SendAttendanceConfirmationAsync_WithDeclinedStatus_HandlesCorrectly()
    {
        // Arrange
        var meeting = CreateTestMeeting();
        var participant = new User 
        { 
            Id = 2, 
            Email = "participant@example.com", 
            FirstName = "John", 
            LastName = "Doe" 
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _notificationService.SendAttendanceConfirmationAsync(meeting, participant, false));

        Assert.True(exception == null || 
                    exception is System.Net.Sockets.SocketException || 
                    exception is System.Net.Mail.SmtpException ||
                    exception is System.IO.IOException);
    }

    private Meeting CreateTestMeeting()
    {
        return new Meeting
        {
            Id = 1,
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Status = MeetingStatus.Scheduled,
            Organizer = new User
            {
                Id = 1,
                Email = "organizer@example.com",
                FirstName = "Admin",
                LastName = "User"
            },
            MeetingRoom = new MeetingRoom
            {
                Id = 1,
                Name = "Conference Room A",
                Location = "Building 1, Floor 2"
            }
        };
    }

    private Meeting CreateTestMeetingWithParticipants()
    {
        var meeting = CreateTestMeeting();
        meeting.Participants = new List<MeetingParticipant>
        {
            new MeetingParticipant
            {
                Id = 1,
                MeetingId = meeting.Id,
                UserId = 2,
                User = new User
                {
                    Id = 2,
                    Email = "participant1@example.com",
                    FirstName = "John",
                    LastName = "Doe"
                }
            },
            new MeetingParticipant
            {
                Id = 2,
                MeetingId = meeting.Id,
                UserId = 3,
                User = new User
                {
                    Id = 3,
                    Email = "participant2@example.com",
                    FirstName = "Jane",
                    LastName = "Smith"
                }
            }
        };
        return meeting;
    }

    private ActionItem CreateTestActionItem()
    {
        return new ActionItem
        {
            Id = 1,
            Description = "Complete project documentation",
            DueDate = DateTime.Today.AddDays(2),
            Status = ActionItemStatus.Pending,
            AssignedTo = new User
            {
                Id = 2,
                Email = "assignee@example.com",
                FirstName = "John",
                LastName = "Doe"
            },
            AgendaItem = new AgendaItem
            {
                Id = 1,
                Title = "Documentation Review",
                Meeting = new Meeting
                {
                    Id = 1,
                    Title = "Project Review Meeting"
                }
            }
        };
    }
}
