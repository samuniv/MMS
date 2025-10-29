using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class EditModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly IRoomService _roomService;
    private readonly UserManager<User> _userManager;
    private readonly IAuditService _auditService;
    private readonly Services.AuditContextService _auditContext;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IMeetingService meetingService,
        IRoomService roomService,
        UserManager<User> userManager,
        IAuditService auditService,
        Services.AuditContextService auditContext,
        ILogger<EditModel> logger)
    {
        _meetingService = meetingService;
        _roomService = roomService;
        _userManager = userManager;
        _auditService = auditService;
        _auditContext = auditContext;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    
    public int MeetingId { get; set; }
    public List<MeetingRoom> AvailableRooms { get; set; } = new();
    public List<User> Users { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        public int? MeetingRoomId { get; set; }

        public MeetingStatus Status { get; set; }

        public List<int> ParticipantIds { get; set; } = new();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        MeetingId = id;
        
        var meeting = await _meetingService.GetMeetingWithDetailsAsync(id);
        if (meeting == null)
        {
            TempData["ErrorMessage"] = "Meeting not found";
            return RedirectToPage("Index");
        }

        // Check permissions
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            if (!User.IsInRole("Administrator") && meeting.OrganizerId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this meeting";
                return RedirectToPage("Details", new { id });
            }
        }

        // Populate form
        Input = new InputModel
        {
            Title = meeting.Title,
            Description = meeting.Description,
            ScheduledDate = meeting.ScheduledDate,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            MeetingRoomId = meeting.MeetingRoomId,
            Status = meeting.Status,
            ParticipantIds = meeting.Participants.Select(p => p.UserId).ToList()
        };

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        MeetingId = id;

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        // Validate times
        if (Input.EndTime <= Input.StartTime)
        {
            ModelState.AddModelError("Input.EndTime", "End time must be after start time");
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id);
            if (meeting == null)
            {
                TempData["ErrorMessage"] = "Meeting not found";
                return RedirectToPage("Index");
            }

            // Check permissions
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                if (!User.IsInRole("Administrator") && meeting.OrganizerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this meeting";
                    return RedirectToPage("Details", new { id });
                }
            }

            // Check room availability if room changed
            if (Input.MeetingRoomId.HasValue && Input.MeetingRoomId != meeting.MeetingRoomId)
            {
                var isAvailable = await _roomService.IsRoomAvailableAsync(
                    Input.MeetingRoomId.Value,
                    Input.ScheduledDate,
                    Input.StartTime,
                    Input.EndTime,
                    id);

                if (!isAvailable)
                {
                    ModelState.AddModelError("Input.MeetingRoomId", "Selected room is not available at this time");
                    await LoadDataAsync();
                    return Page();
                }
            }

            var dto = new UpdateMeetingDto
            {
                Title = Input.Title,
                Description = Input.Description,
                ScheduledDate = Input.ScheduledDate,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                MeetingRoomId = Input.MeetingRoomId,
                Status = Input.Status,
                ParticipantIds = Input.ParticipantIds
            };

            var oldMeeting = meeting;
            var updatedMeeting = await _meetingService.UpdateMeetingAsync(id, dto);
            
            // Log audit trail
            await _auditService.LogUpdateAsync(oldMeeting, updatedMeeting, _auditContext.GetCurrentUserId(), _auditContext.GetIpAddress());
            
            _logger.LogInformation("Meeting {MeetingId} updated", id);
            TempData["SuccessMessage"] = "Meeting updated successfully!";
            
            return RedirectToPage("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meeting {MeetingId}", id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the meeting");
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var rooms = await _roomService.GetActiveRoomsAsync();
            AvailableRooms = rooms.ToList();

            var allUsers = _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToList();
            Users = allUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data for meeting edit");
        }
    }
}
