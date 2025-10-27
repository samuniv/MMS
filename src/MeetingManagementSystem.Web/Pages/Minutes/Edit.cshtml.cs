using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Minutes;

[Authorize]
public class EditModel : PageModel
{
    private readonly IMeetingMinutesService _minutesService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IMeetingMinutesService minutesService,
        IMeetingService meetingService,
        ILogger<EditModel> logger)
    {
        _minutesService = minutesService;
        _meetingService = meetingService;
        _logger = logger;
    }

    [BindProperty]
    public int MeetingId { get; set; }

    [BindProperty]
    public new string Content { get; set; } = string.Empty;

    public Meeting? Meeting { get; set; }
    public MeetingMinutes? ExistingMinutes { get; set; }

    public async Task<IActionResult> OnGetAsync(int meetingId)
    {
        MeetingId = meetingId;
        Meeting = await _meetingService.GetMeetingWithDetailsAsync(meetingId);

        if (Meeting == null)
        {
            return NotFound();
        }

        ExistingMinutes = await _minutesService.GetMinutesByMeetingIdAsync(meetingId);
        if (ExistingMinutes != null)
        {
            Content = ExistingMinutes.Content;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var existingMinutes = await _minutesService.GetMinutesByMeetingIdAsync(MeetingId);

            if (existingMinutes != null)
            {
                // Update existing minutes
                var updateDto = new UpdateMeetingMinutesDto
                {
                    Content = Content
                };
                await _minutesService.UpdateMinutesAsync(existingMinutes.Id, updateDto);
                TempData["SuccessMessage"] = "Meeting minutes updated successfully";
            }
            else
            {
                // Create new minutes
                var createDto = new CreateMeetingMinutesDto
                {
                    MeetingId = MeetingId,
                    Content = Content,
                    CreatedById = userId
                };
                await _minutesService.CreateMinutesAsync(createDto);
                TempData["SuccessMessage"] = "Meeting minutes created successfully";
            }

            return RedirectToPage("/Meetings/Details", new { id = MeetingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving meeting minutes for meeting {MeetingId}", MeetingId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving the minutes");
            return Page();
        }
    }
}
