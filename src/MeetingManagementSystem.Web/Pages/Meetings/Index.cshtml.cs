using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Web.Pages.Meetings;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMeetingService meetingService, ILogger<IndexModel> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    public List<Meeting> Meetings { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var startDate = FromDate ?? DateTime.Today.AddMonths(-1);
            var endDate = ToDate ?? DateTime.Today.AddMonths(3);
            
            var meetings = await _meetingService.GetMeetingsByDateRangeAsync(startDate, endDate);
            
            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                meetings = meetings.Where(m => 
                    m.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrWhiteSpace(StatusFilter) && Enum.TryParse<MeetingStatus>(StatusFilter, out var status))
            {
                meetings = meetings.Where(m => m.Status == status);
            }
            
            Meetings = meetings.OrderBy(m => m.ScheduledDate).ThenBy(m => m.StartTime).ToList();
            
            _logger.LogInformation("Retrieved {Count} meetings", Meetings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meetings");
            TempData["ErrorMessage"] = "An error occurred while loading meetings.";
        }
    }
}
