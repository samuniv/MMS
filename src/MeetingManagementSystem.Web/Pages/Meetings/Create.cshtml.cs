using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize(Roles = "Administrator,Government Official")]
public class CreateModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly IRoomService _roomService;
    private readonly UserManager<User> _userManager;
    private readonly IAuditService _auditService;
    private readonly Services.AuditContextService _auditContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IMeetingService meetingService,
        IRoomService roomService,
        UserManager<User> userManager,
        IAuditService auditService,
        Services.AuditContextService auditContext,
        ILogger<CreateModel> logger)
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
    
    public List<MeetingRoom> AvailableRooms { get; set; } = new();
    public List<User> Users { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Meeting title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Meeting date is required")]
        [DataType(DataType.Date)]
        public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } = new TimeSpan(10, 0, 0);

        [Required(ErrorMessage = "End time is required")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; } = new TimeSpan(11, 0, 0);

        public int? MeetingRoomId { get; set; }

        public List<int> ParticipantIds { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
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

        // Validate date
        if (Input.ScheduledDate < DateTime.Today)
        {
            ModelState.AddModelError("Input.ScheduledDate", "Cannot schedule meetings in the past");
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                ModelState.AddModelError(string.Empty, "Unable to identify user");
                await LoadDataAsync();
                return Page();
            }

            // Check room availability if room is selected
            if (Input.MeetingRoomId.HasValue)
            {
                var isAvailable = await _roomService.IsRoomAvailableAsync(
                    Input.MeetingRoomId.Value,
                    Input.ScheduledDate,
                    Input.StartTime,
                    Input.EndTime);

                if (!isAvailable)
                {
                    ModelState.AddModelError("Input.MeetingRoomId", "Selected room is not available at this time");
                    await LoadDataAsync();
                    return Page();
                }
            }

            var dto = new CreateMeetingDto
            {
                Title = Input.Title,
                Description = Input.Description,
                ScheduledDate = Input.ScheduledDate,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                OrganizerId = userId,
                MeetingRoomId = Input.MeetingRoomId,
                ParticipantIds = Input.ParticipantIds
            };

            var meeting = await _meetingService.CreateMeetingAsync(dto);
            
            // Log audit trail
            await _auditService.LogCreateAsync(meeting, _auditContext.GetCurrentUserId(), _auditContext.GetIpAddress());
            
            _logger.LogInformation("Meeting {MeetingId} created by user {UserId}", meeting.Id, userId);
            TempData["SuccessMessage"] = "Meeting scheduled successfully!";
            
            return RedirectToPage("Details", new { id = meeting.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meeting");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the meeting");
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
            _logger.LogError(ex, "Error loading data for meeting creation");
        }
    }
}
