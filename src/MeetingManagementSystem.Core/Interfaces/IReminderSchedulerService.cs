namespace MeetingManagementSystem.Core.Interfaces;

public interface IReminderSchedulerService
{
    Task ScheduleMeetingRemindersAsync(int meetingId);
    Task ScheduleActionItemRemindersAsync(int actionItemId);
    Task ProcessPendingRemindersAsync();
    Task CancelMeetingRemindersAsync(int meetingId);
}
