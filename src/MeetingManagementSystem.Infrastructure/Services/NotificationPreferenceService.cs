using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Services;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationPreferenceService> _logger;

    public NotificationPreferenceService(
        ApplicationDbContext context,
        ILogger<NotificationPreferenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationPreference?> GetUserPreferencesAsync(int userId)
    {
        try
        {
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);

            if (preferences == null)
            {
                _logger.LogInformation("No preferences found for user {UserId}, creating defaults", userId);
                preferences = await CreateDefaultPreferencesAsync(userId);
            }

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationPreference> CreateDefaultPreferencesAsync(int userId)
    {
        try
        {
            var preferences = new NotificationPreference
            {
                UserId = userId,
                MeetingInvitations = true,
                MeetingReminders = true,
                MeetingUpdates = true,
                MeetingCancellations = true,
                ActionItemAssignments = true,
                ActionItemReminders = true,
                ActionItemUpdates = true,
                EmailNotifications = true,
                SystemNotifications = true,
                Reminder24Hours = true,
                Reminder1Hour = true
            };

            _context.NotificationPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default notification preferences for user {UserId}", userId);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationPreference> UpdatePreferencesAsync(int userId, NotificationPreference preferences)
    {
        try
        {
            var existing = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);

            if (existing == null)
            {
                preferences.UserId = userId;
                _context.NotificationPreferences.Add(preferences);
            }
            else
            {
                existing.MeetingInvitations = preferences.MeetingInvitations;
                existing.MeetingReminders = preferences.MeetingReminders;
                existing.MeetingUpdates = preferences.MeetingUpdates;
                existing.MeetingCancellations = preferences.MeetingCancellations;
                existing.ActionItemAssignments = preferences.ActionItemAssignments;
                existing.ActionItemReminders = preferences.ActionItemReminders;
                existing.ActionItemUpdates = preferences.ActionItemUpdates;
                existing.EmailNotifications = preferences.EmailNotifications;
                existing.SystemNotifications = preferences.SystemNotifications;
                existing.Reminder24Hours = preferences.Reminder24Hours;
                existing.Reminder1Hour = preferences.Reminder1Hour;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

            return existing ?? preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationHistory>> GetUserNotificationHistoryAsync(int userId, int pageSize = 50)
    {
        try
        {
            return await _context.NotificationHistories
                .Where(nh => nh.UserId == userId)
                .OrderByDescending(nh => nh.SentAt)
                .Take(pageSize)
                .Include(nh => nh.RelatedMeeting)
                .Include(nh => nh.RelatedActionItem)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationHistory> LogNotificationAsync(NotificationHistory notification)
    {
        try
        {
            _context.NotificationHistories.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging notification for user {UserId}", notification.UserId);
            throw;
        }
    }

    public async Task<bool> ShouldSendNotificationAsync(int userId, string notificationType)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            if (preferences == null) return true; // Default to sending if no preferences

            return notificationType switch
            {
                "MeetingInvitation" => preferences.MeetingInvitations,
                "MeetingReminder" => preferences.MeetingReminders,
                "MeetingUpdate" => preferences.MeetingUpdates,
                "MeetingCancellation" => preferences.MeetingCancellations,
                "ActionItemAssignment" => preferences.ActionItemAssignments,
                "ActionItemReminder" => preferences.ActionItemReminders,
                "ActionItemUpdate" => preferences.ActionItemUpdates,
                _ => true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking notification preferences for user {UserId}", userId);
            return true; // Default to sending on error
        }
    }
}
