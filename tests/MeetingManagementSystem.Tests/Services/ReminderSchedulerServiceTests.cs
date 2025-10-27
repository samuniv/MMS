using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class ReminderSchedulerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ReminderSchedulerService>> _loggerMock;
    private readonly ReminderSchedulerService _reminderScheduler;

    public ReminderSchedulerServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<ReminderSchedulerService>>();

        _reminderScheduler = new ReminderSchedulerService(
            _context,
            _notificationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ScheduleMeetingRemindersAsync_WithFutureMeeting_CreatesReminders()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddDays(2));
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        // Act
        await _reminderScheduler.ScheduleMeetingRemindersAsync(meeting.Id);

        // Assert
        var reminders = await _context.ScheduledReminders
            .Where(r => r.MeetingId == meeting.Id)
            .ToListAsync();

        Assert.Equal(2, reminders.Count);
        Assert.Contains(reminders, r => r.Type == ReminderType.MeetingReminder24Hours);
        Assert.Contains(reminders, r => r.Type == ReminderType.MeetingReminder1Hour);
    }

    [Fact]
    public async Task ScheduleMeetingRemindersAsync_WithPastMeeting_CreatesNoReminders()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddHours(-2));
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        // Act
        await _reminderScheduler.ScheduleMeetingRemindersAsync(meeting.Id);

        // Assert
        var reminders = await _context.ScheduledReminders
            .Where(r => r.MeetingId == meeting.Id)
            .ToListAsync();

        Assert.Empty(reminders);
    }

    [Fact]
    public async Task ScheduleActionItemRemindersAsync_WithFutureDeadline_CreatesReminders()
    {
        // Arrange
        var actionItem = CreateTestActionItem(DateTime.UtcNow.AddDays(3));
        _context.ActionItems.Add(actionItem);
        await _context.SaveChangesAsync();

        // Act
        await _reminderScheduler.ScheduleActionItemRemindersAsync(actionItem.Id);

        // Assert
        var reminders = await _context.ScheduledReminders
            .Where(r => r.ActionItemId == actionItem.Id)
            .ToListAsync();

        Assert.Equal(2, reminders.Count);
        Assert.Contains(reminders, r => r.Type == ReminderType.ActionItemReminder48Hours);
        Assert.Contains(reminders, r => r.Type == ReminderType.ActionItemReminder24Hours);
    }

    [Fact]
    public async Task ProcessPendingRemindersAsync_WithDueReminders_SendsNotifications()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddHours(1));
        _context.Meetings.Add(meeting);
        
        var reminder = new ScheduledReminder
        {
            Type = ReminderType.MeetingReminder1Hour,
            MeetingId = meeting.Id,
            ScheduledTime = DateTime.UtcNow.AddMinutes(-5),
            IsSent = false
        };
        _context.ScheduledReminders.Add(reminder);
        await _context.SaveChangesAsync();

        _notificationServiceMock
            .Setup(n => n.SendMeetingReminderAsync(It.IsAny<Meeting>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        await _reminderScheduler.ProcessPendingRemindersAsync();

        // Assert
        var updatedReminder = await _context.ScheduledReminders.FindAsync(reminder.Id);
        Assert.NotNull(updatedReminder);
        Assert.True(updatedReminder.IsSent);
        Assert.NotNull(updatedReminder.SentAt);
        _notificationServiceMock.Verify(
            n => n.SendMeetingReminderAsync(It.IsAny<Meeting>(), It.IsAny<TimeSpan>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingRemindersAsync_WithFailedReminder_IncrementsRetryCount()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddHours(1));
        _context.Meetings.Add(meeting);
        
        var reminder = new ScheduledReminder
        {
            Type = ReminderType.MeetingReminder1Hour,
            MeetingId = meeting.Id,
            ScheduledTime = DateTime.UtcNow.AddMinutes(-5),
            IsSent = false,
            RetryCount = 0
        };
        _context.ScheduledReminders.Add(reminder);
        await _context.SaveChangesAsync();

        _notificationServiceMock
            .Setup(n => n.SendMeetingReminderAsync(It.IsAny<Meeting>(), It.IsAny<TimeSpan>()))
            .ThrowsAsync(new Exception("SMTP error"));

        // Act
        await _reminderScheduler.ProcessPendingRemindersAsync();

        // Assert
        var updatedReminder = await _context.ScheduledReminders.FindAsync(reminder.Id);
        Assert.NotNull(updatedReminder);
        Assert.False(updatedReminder.IsSent);
        Assert.Equal(1, updatedReminder.RetryCount);
        Assert.NotNull(updatedReminder.ErrorMessage);
    }

    [Fact]
    public async Task CancelMeetingRemindersAsync_WithExistingReminders_RemovesThem()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddDays(2));
        _context.Meetings.Add(meeting);
        
        var reminders = new List<ScheduledReminder>
        {
            new ScheduledReminder
            {
                Type = ReminderType.MeetingReminder24Hours,
                MeetingId = meeting.Id,
                ScheduledTime = DateTime.UtcNow.AddDays(1),
                IsSent = false
            },
            new ScheduledReminder
            {
                Type = ReminderType.MeetingReminder1Hour,
                MeetingId = meeting.Id,
                ScheduledTime = DateTime.UtcNow.AddDays(1).AddHours(23),
                IsSent = false
            }
        };
        _context.ScheduledReminders.AddRange(reminders);
        await _context.SaveChangesAsync();

        // Act
        await _reminderScheduler.CancelMeetingRemindersAsync(meeting.Id);

        // Assert
        var remainingReminders = await _context.ScheduledReminders
            .Where(r => r.MeetingId == meeting.Id)
            .ToListAsync();

        Assert.Empty(remainingReminders);
    }

    [Fact]
    public async Task ProcessPendingRemindersAsync_SkipsAlreadySentReminders()
    {
        // Arrange
        var meeting = CreateTestMeeting(DateTime.UtcNow.AddHours(1));
        _context.Meetings.Add(meeting);
        
        var reminder = new ScheduledReminder
        {
            Type = ReminderType.MeetingReminder1Hour,
            MeetingId = meeting.Id,
            ScheduledTime = DateTime.UtcNow.AddMinutes(-5),
            IsSent = true,
            SentAt = DateTime.UtcNow.AddMinutes(-3)
        };
        _context.ScheduledReminders.Add(reminder);
        await _context.SaveChangesAsync();

        // Act
        await _reminderScheduler.ProcessPendingRemindersAsync();

        // Assert
        _notificationServiceMock.Verify(
            n => n.SendMeetingReminderAsync(It.IsAny<Meeting>(), It.IsAny<TimeSpan>()), 
            Times.Never);
    }

    private Meeting CreateTestMeeting(DateTime scheduledDateTime)
    {
        return new Meeting
        {
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = scheduledDateTime.Date,
            StartTime = scheduledDateTime.TimeOfDay,
            EndTime = scheduledDateTime.AddHours(1).TimeOfDay,
            OrganizerId = 1,
            Status = MeetingStatus.Scheduled,
            Organizer = new User
            {
                Id = 1,
                Email = "organizer@example.com",
                FirstName = "Admin",
                LastName = "User",
                UserName = "admin"
            },
            MeetingRoom = new MeetingRoom
            {
                Id = 1,
                Name = "Conference Room A",
                Location = "Building 1"
            }
        };
    }

    private ActionItem CreateTestActionItem(DateTime dueDate)
    {
        var user = new User
        {
            Id = 2,
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser"
        };
        _context.Users.Add(user);

        var organizer = new User
        {
            Id = 3,
            Email = "organizer@example.com",
            FirstName = "Admin",
            LastName = "User",
            UserName = "admin"
        };
        _context.Users.Add(organizer);

        var meeting = new Meeting
        {
            Title = "Test Meeting",
            Description = "Test Description",
            ScheduledDate = DateTime.UtcNow.AddDays(1).Date,
            StartTime = DateTime.UtcNow.AddDays(1).TimeOfDay,
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1).TimeOfDay,
            OrganizerId = organizer.Id,
            Status = MeetingStatus.Scheduled
        };
        _context.Meetings.Add(meeting);

        var agendaItem = new AgendaItem
        {
            Title = "Test Agenda",
            Description = "Test",
            Meeting = meeting,
            OrderIndex = 1
        };
        _context.AgendaItems.Add(agendaItem);

        return new ActionItem
        {
            Description = "Test Action Item",
            DueDate = dueDate,
            Status = ActionItemStatus.Pending,
            AgendaItem = agendaItem,
            AssignedToId = user.Id
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
