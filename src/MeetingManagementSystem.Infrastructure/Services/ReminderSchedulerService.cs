using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Services;

public class ReminderSchedulerService : IReminderSchedulerService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReminderSchedulerService> _logger;

    public ReminderSchedulerService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ReminderSchedulerService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ScheduleMeetingRemindersAsync(int meetingId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.MeetingRoom)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            _logger.LogWarning("Cannot schedule reminders - meeting {MeetingId} not found", meetingId);
            return;
        }

        var meetingDateTime = meeting.ScheduledDate.Add(meeting.StartTime);

        // Schedule 24-hour reminder
        var reminder24h = new ScheduledReminder
        {
            Type = ReminderType.MeetingReminder24Hours,
            MeetingId = meetingId,
            ScheduledTime = meetingDateTime.AddHours(-24)
        };

        // Schedule 1-hour reminder
        var reminder1h = new ScheduledReminder
        {
            Type = ReminderType.MeetingReminder1Hour,
            MeetingId = meetingId,
            ScheduledTime = meetingDateTime.AddHours(-1)
        };

        // Only add reminders if they are in the future
        if (reminder24h.ScheduledTime > DateTime.UtcNow)
        {
            _context.ScheduledReminders.Add(reminder24h);
            _logger.LogInformation("Scheduled 24-hour reminder for meeting {MeetingId} at {Time}",
                meetingId, reminder24h.ScheduledTime);
        }

        if (reminder1h.ScheduledTime > DateTime.UtcNow)
        {
            _context.ScheduledReminders.Add(reminder1h);
            _logger.LogInformation("Scheduled 1-hour reminder for meeting {MeetingId} at {Time}",
                meetingId, reminder1h.ScheduledTime);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ScheduleActionItemRemindersAsync(int actionItemId)
    {
        var actionItem = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .Include(a => a.AgendaItem)
                .ThenInclude(ai => ai.Meeting)
            .FirstOrDefaultAsync(a => a.Id == actionItemId);

        if (actionItem == null)
        {
            _logger.LogWarning("Cannot schedule reminders - action item {ActionItemId} not found", actionItemId);
            return;
        }

        // Schedule 48-hour reminder
        var reminder48h = new ScheduledReminder
        {
            Type = ReminderType.ActionItemReminder48Hours,
            ActionItemId = actionItemId,
            ScheduledTime = actionItem.DueDate.AddHours(-48)
        };

        // Schedule 24-hour reminder
        var reminder24h = new ScheduledReminder
        {
            Type = ReminderType.ActionItemReminder24Hours,
            ActionItemId = actionItemId,
            ScheduledTime = actionItem.DueDate.AddHours(-24)
        };

        // Only add reminders if they are in the future
        if (reminder48h.ScheduledTime > DateTime.UtcNow)
        {
            _context.ScheduledReminders.Add(reminder48h);
            _logger.LogInformation("Scheduled 48-hour reminder for action item {ActionItemId} at {Time}",
                actionItemId, reminder48h.ScheduledTime);
        }

        if (reminder24h.ScheduledTime > DateTime.UtcNow)
        {
            _context.ScheduledReminders.Add(reminder24h);
            _logger.LogInformation("Scheduled 24-hour reminder for action item {ActionItemId} at {Time}",
                actionItemId, reminder24h.ScheduledTime);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ProcessPendingRemindersAsync()
    {
        var now = DateTime.UtcNow;
        
        var pendingReminders = await _context.ScheduledReminders
            .Include(r => r.Meeting)
                .ThenInclude(m => m!.Organizer)
            .Include(r => r.Meeting)
                .ThenInclude(m => m!.MeetingRoom)
            .Include(r => r.Meeting)
                .ThenInclude(m => m!.Participants)
                    .ThenInclude(p => p.User)
            .Include(r => r.ActionItem)
                .ThenInclude(a => a!.AssignedTo)
            .Include(r => r.ActionItem)
                .ThenInclude(a => a!.AgendaItem)
                    .ThenInclude(ai => ai.Meeting)
            .Where(r => !r.IsSent && r.ScheduledTime <= now && r.RetryCount < 3)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} pending reminders", pendingReminders.Count);

        foreach (var reminder in pendingReminders)
        {
            try
            {
                await ProcessReminderAsync(reminder);
                
                reminder.IsSent = true;
                reminder.SentAt = DateTime.UtcNow;
                reminder.ErrorMessage = null;
                
                _logger.LogInformation("Successfully processed reminder {ReminderId} of type {Type}",
                    reminder.Id, reminder.Type);
            }
            catch (Exception ex)
            {
                reminder.RetryCount++;
                reminder.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "Failed to process reminder {ReminderId} (attempt {Attempt}/3)",
                    reminder.Id, reminder.RetryCount);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelMeetingRemindersAsync(int meetingId)
    {
        var reminders = await _context.ScheduledReminders
            .Where(r => r.MeetingId == meetingId && !r.IsSent)
            .ToListAsync();

        _context.ScheduledReminders.RemoveRange(reminders);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled {Count} reminders for meeting {MeetingId}",
            reminders.Count, meetingId);
    }

    private async Task ProcessReminderAsync(ScheduledReminder reminder)
    {
        switch (reminder.Type)
        {
            case ReminderType.MeetingReminder24Hours:
                if (reminder.Meeting != null)
                {
                    await _notificationService.SendMeetingReminderAsync(
                        reminder.Meeting, TimeSpan.FromHours(24));
                }
                break;

            case ReminderType.MeetingReminder1Hour:
                if (reminder.Meeting != null)
                {
                    await _notificationService.SendMeetingReminderAsync(
                        reminder.Meeting, TimeSpan.FromHours(1));
                }
                break;

            case ReminderType.ActionItemReminder48Hours:
            case ReminderType.ActionItemReminder24Hours:
                if (reminder.ActionItem != null)
                {
                    await _notificationService.SendActionItemReminderAsync(reminder.ActionItem);
                }
                break;

            default:
                _logger.LogWarning("Unknown reminder type: {Type}", reminder.Type);
                break;
        }
    }
}
