using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface INotificationPreferenceService
{
    Task<NotificationPreference?> GetUserPreferencesAsync(int userId);
    Task<NotificationPreference> CreateDefaultPreferencesAsync(int userId);
    Task<NotificationPreference> UpdatePreferencesAsync(int userId, NotificationPreference preferences);
    Task<IEnumerable<NotificationHistory>> GetUserNotificationHistoryAsync(int userId, int pageSize = 50);
    Task<NotificationHistory> LogNotificationAsync(NotificationHistory notification);
    Task<bool> ShouldSendNotificationAsync(int userId, string notificationType);
}
