using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class MyMeetingsModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<MyMeetingsModel> _logger;

    public MyMeetingsModel(IMeetingService meetingService, ILogger<MyMeetingsModel> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    public List<Meeting> Meetings { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "upcoming";

    public async Task OnGetAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Unable to get user ID from claims");
                return;
            }

            var allMeetings = await _meetingService.GetUserMeetingsAsync(userId);
            
            Meetings = ActiveTab switch
            {
                "upcoming" => allMeetings
                    .Where(m => m.ScheduledDate >= DateTime.Today && m.Status != Core.Enums.MeetingStatus.Cancelled)
                    .OrderBy(m => m.ScheduledDate)
                    .ThenBy(m => m.StartTime)
                    .ToList(),
                    
                "past" => allMeetings
                    .Where(m => m.ScheduledDate < DateTime.Today || m.Status == Core.Enums.MeetingStatus.Completed)
                    .OrderByDescending(m => m.ScheduledDate)
                    .ThenByDescending(m => m.StartTime)
                    .ToList(),
                    
                "organized" => allMeetings
                    .Where(m => m.OrganizerId == userId)
                    .OrderByDescending(m => m.ScheduledDate)
                    .ThenByDescending(m => m.StartTime)
                    .ToList(),
                    
                _ => allMeetings.OrderBy(m => m.ScheduledDate).ThenBy(m => m.StartTime).ToList()
            };
            
            _logger.LogInformation("Retrieved {Count} meetings for user {UserId} (tab: {Tab})", 
                Meetings.Count, userId, ActiveTab);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user meetings");
            TempData["ErrorMessage"] = "An error occurred while loading your meetings.";
        }
    }
}
