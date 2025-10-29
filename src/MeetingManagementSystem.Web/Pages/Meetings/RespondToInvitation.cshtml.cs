using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class RespondToInvitationModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RespondToInvitationModel> _logger;

    public RespondToInvitationModel(
        IMeetingService meetingService,
        INotificationService notificationService,
        ILogger<RespondToInvitationModel> logger)
    {
        _meetingService = meetingService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Meeting? Meeting { get; set; }
    public MeetingParticipant? CurrentParticipant { get; set; }

    [BindProperty]
    public string? Comment { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Meeting = await _meetingService.GetMeetingWithDetailsAsync(id);
            
            if (Meeting == null)
            {
                TempData["ErrorMessage"] = "Meeting not found";
                return RedirectToPage("MyMeetings");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user";
                return RedirectToPage("MyMeetings");
            }

            CurrentParticipant = Meeting.Participants.FirstOrDefault(p => p.UserId == userId);
            
            if (CurrentParticipant == null)
            {
                TempData["ErrorMessage"] = "You are not invited to this meeting";
                return RedirectToPage("MyMeetings");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading meeting invitation {MeetingId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the invitation";
            return RedirectToPage("MyMeetings");
        }
    }

    public async Task<IActionResult> OnPostAcceptAsync(int id)
    {
        return await RespondToInvitationAsync(id, AttendanceStatus.Accepted);
    }

    public async Task<IActionResult> OnPostDeclineAsync(int id)
    {
        return await RespondToInvitationAsync(id, AttendanceStatus.Declined);
    }

    private async Task<IActionResult> RespondToInvitationAsync(int meetingId, AttendanceStatus status)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user";
                return RedirectToPage("MyMeetings");
            }

            var meeting = await _meetingService.GetMeetingWithDetailsAsync(meetingId);
            if (meeting == null)
            {
                TempData["ErrorMessage"] = "Meeting not found";
                return RedirectToPage("MyMeetings");
            }

            var participant = meeting.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant == null)
            {
                TempData["ErrorMessage"] = "You are not invited to this meeting";
                return RedirectToPage("MyMeetings");
            }

            var success = await _meetingService.UpdateParticipantStatusAsync(meetingId, userId, status);
            
            if (success)
            {
                var user = participant.User;
                await _notificationService.SendAttendanceConfirmationAsync(meeting, user, status == AttendanceStatus.Accepted);
                
                var statusText = status == AttendanceStatus.Accepted ? "accepted" : "declined";
                _logger.LogInformation("User {UserId} {Status} meeting invitation {MeetingId}", userId, statusText, meetingId);
                TempData["SuccessMessage"] = $"You have {statusText} the meeting invitation";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update your response";
            }

            return RedirectToPage("Details", new { id = meetingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to meeting invitation {MeetingId}", meetingId);
            TempData["ErrorMessage"] = "An error occurred while processing your response";
            return RedirectToPage("MyMeetings");
        }
    }
}
