using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Account;

[Authorize]
public class NotificationHistoryModel : PageModel
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly ILogger<NotificationHistoryModel> _logger;

    public NotificationHistoryModel(
        INotificationPreferenceService preferenceService,
        ILogger<NotificationHistoryModel> logger)
    {
        _preferenceService = preferenceService;
        _logger = logger;
    }

    public IEnumerable<NotificationHistory> Notifications { get; set; } = new List<NotificationHistory>();

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

            Notifications = await _preferenceService.GetUserNotificationHistoryAsync(userId, 100);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification history");
            TempData["ErrorMessage"] = "An error occurred while loading notification history";
            return RedirectToPage("/Index");
        }
    }
}
