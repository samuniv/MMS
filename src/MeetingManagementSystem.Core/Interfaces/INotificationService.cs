using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface INotificationService
{
    Task SendMeetingInvitationAsync(Meeting meeting, IEnumerable<User> participants);
    Task SendMeetingReminderAsync(Meeting meeting, TimeSpan reminderTime);
    Task SendMeetingCancellationAsync(Meeting meeting, string reason);
    Task SendActionItemReminderAsync(ActionItem actionItem);
    Task SendMeetingUpdateNotificationAsync(Meeting meeting, string updateMessage);
    Task SendAttendanceConfirmationAsync(Meeting meeting, User participant, bool isAccepted);
}
