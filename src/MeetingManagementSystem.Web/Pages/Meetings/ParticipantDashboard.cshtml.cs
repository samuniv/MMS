using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class ParticipantDashboardModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<ParticipantDashboardModel> _logger;

    public ParticipantDashboardModel(
        IMeetingService meetingService,
        ILogger<ParticipantDashboardModel> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    public List<MeetingInvitation> PendingInvitations { get; set; } = new();
    public List<MeetingInvitation> UpcomingMeetings { get; set; } = new();
    public List<MeetingInvitation> PastMeetings { get; set; } = new();
    public Dictionary<AttendanceStatus, int> AttendanceStats { get; set; } = new();

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

            var allMeetings = await _meetingService.GetUserMeetingsAsync(userId);
            var now = DateTime.Now;
            var today = DateTime.Today;

            foreach (var meeting in allMeetings)
            {
                var participant = meeting.Participants.FirstOrDefault(p => p.UserId == userId);
                if (participant == null) continue;

                var invitation = new MeetingInvitation
                {
                    Meeting = meeting,
                    Participant = participant
                };

                var meetingDateTime = meeting.ScheduledDate.Date + meeting.StartTime;

                if (participant.AttendanceStatus == AttendanceStatus.Pending && meetingDateTime >= now)
                {
                    PendingInvitations.Add(invitation);
                }
                else if (meetingDateTime >= now && meeting.Status != MeetingStatus.Cancelled)
                {
                    UpcomingMeetings.Add(invitation);
                }
                else if (meetingDateTime < now || meeting.Status == MeetingStatus.Completed)
                {
                    PastMeetings.Add(invitation);
                }
            }

            // Sort lists
            PendingInvitations = PendingInvitations
                .OrderBy(i => i.Meeting.ScheduledDate)
                .ThenBy(i => i.Meeting.StartTime)
                .ToList();

            UpcomingMeetings = UpcomingMeetings
                .OrderBy(i => i.Meeting.ScheduledDate)
                .ThenBy(i => i.Meeting.StartTime)
                .ToList();

            PastMeetings = PastMeetings
                .OrderByDescending(i => i.Meeting.ScheduledDate)
                .ThenByDescending(i => i.Meeting.StartTime)
                .Take(10)
                .ToList();

            // Calculate stats
            var allParticipations = allMeetings
                .SelectMany(m => m.Participants.Where(p => p.UserId == userId))
                .ToList();

            AttendanceStats = allParticipations
                .GroupBy(p => p.AttendanceStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading participant dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading your dashboard";
            return RedirectToPage("/Index");
        }
    }

    public class MeetingInvitation
    {
        public Meeting Meeting { get; set; } = null!;
        public MeetingParticipant Participant { get; set; } = null!;
    }
}
