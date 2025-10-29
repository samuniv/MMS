using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Account;

[Authorize]
public class NotificationSettingsModel : PageModel
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly ILogger<NotificationSettingsModel> _logger;

    public NotificationSettingsModel(
        INotificationPreferenceService preferenceService,
        ILogger<NotificationSettingsModel> logger)
    {
        _preferenceService = preferenceService;
        _logger = logger;
    }

    [BindProperty]
    public NotificationPreferenceViewModel Preferences { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user";
                return RedirectToPage("/Index");
            }

            var preferences = await _preferenceService.GetUserPreferencesAsync(userId);
            if (preferences != null)
            {
                Preferences = new NotificationPreferenceViewModel
                {
                    MeetingInvitations = preferences.MeetingInvitations,
                    MeetingReminders = preferences.MeetingReminders,
                    MeetingUpdates = preferences.MeetingUpdates,
                    MeetingCancellations = preferences.MeetingCancellations,
                    ActionItemAssignments = preferences.ActionItemAssignments,
                    ActionItemReminders = preferences.ActionItemReminders,
                    ActionItemUpdates = preferences.ActionItemUpdates,
                    EmailNotifications = preferences.EmailNotifications,
                    SystemNotifications = preferences.SystemNotifications,
                    Reminder24Hours = preferences.Reminder24Hours,
                    Reminder1Hour = preferences.Reminder1Hour
                };
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification settings");
            TempData["ErrorMessage"] = "An error occurred while loading your settings";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user";
                return RedirectToPage("/Index");
            }

            var preferences = new NotificationPreference
            {
                UserId = userId,
                MeetingInvitations = Preferences.MeetingInvitations,
                MeetingReminders = Preferences.MeetingReminders,
                MeetingUpdates = Preferences.MeetingUpdates,
                MeetingCancellations = Preferences.MeetingCancellations,
                ActionItemAssignments = Preferences.ActionItemAssignments,
                ActionItemReminders = Preferences.ActionItemReminders,
                ActionItemUpdates = Preferences.ActionItemUpdates,
                EmailNotifications = Preferences.EmailNotifications,
                SystemNotifications = Preferences.SystemNotifications,
                Reminder24Hours = Preferences.Reminder24Hours,
                Reminder1Hour = Preferences.Reminder1Hour
            };

            await _preferenceService.UpdatePreferencesAsync(userId, preferences);
            
            _logger.LogInformation("User {UserId} updated notification preferences", userId);
            TempData["SuccessMessage"] = "Notification settings updated successfully";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            TempData["ErrorMessage"] = "An error occurred while updating your settings";
            return Page();
        }
    }

    public class NotificationPreferenceViewModel
    {
        public bool MeetingInvitations { get; set; } = true;
        public bool MeetingReminders { get; set; } = true;
        public bool MeetingUpdates { get; set; } = true;
        public bool MeetingCancellations { get; set; } = true;
        public bool ActionItemAssignments { get; set; } = true;
        public bool ActionItemReminders { get; set; } = true;
        public bool ActionItemUpdates { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
        public bool Reminder24Hours { get; set; } = true;
        public bool Reminder1Hour { get; set; } = true;
    }
}
