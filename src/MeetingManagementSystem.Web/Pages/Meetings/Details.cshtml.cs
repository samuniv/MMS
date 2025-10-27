using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IMeetingService meetingService, ILogger<DetailsModel> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    public Meeting? Meeting { get; set; }
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Meeting = await _meetingService.GetMeetingWithDetailsAsync(id);
            
            if (Meeting == null)
            {
                TempData["ErrorMessage"] = "Meeting not found";
                return RedirectToPage("Index");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                CanEdit = User.IsInRole("Administrator") || Meeting.OrganizerId == userId;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading meeting {MeetingId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the meeting";
            return RedirectToPage("Index");
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id);
            if (meeting == null)
            {
                TempData["ErrorMessage"] = "Meeting not found";
                return RedirectToPage("Index");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user";
                return RedirectToPage("Details", new { id });
            }

            if (!User.IsInRole("Administrator") && meeting.OrganizerId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to cancel this meeting";
                return RedirectToPage("Details", new { id });
            }

            var success = await _meetingService.CancelMeetingAsync(id, "Cancelled by organizer");
            
            if (success)
            {
                _logger.LogInformation("Meeting {MeetingId} cancelled by user {UserId}", id, userId);
                TempData["SuccessMessage"] = "Meeting cancelled successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel meeting";
            }

            return RedirectToPage("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling meeting {MeetingId}", id);
            TempData["ErrorMessage"] = "An error occurred while cancelling the meeting";
            return RedirectToPage("Details", new { id });
        }
    }
}
