using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Rooms;

[Authorize]
public class CalendarModel : PageModel
{
    private readonly IRoomService _roomService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<CalendarModel> _logger;

    public CalendarModel(
        IRoomService roomService,
        IMeetingService meetingService,
        ILogger<CalendarModel> logger)
    {
        _roomService = roomService;
        _meetingService = meetingService;
        _logger = logger;
    }

    public List<MeetingRoom> AllRooms { get; set; } = new();
    public List<Meeting> Bookings { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public int? SelectedRoomId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime SelectedDate { get; set; } = DateTime.Today;

    public async Task OnGetAsync()
    {
        try
        {
            // Load all rooms
            var rooms = await _roomService.GetActiveRoomsAsync();
            AllRooms = rooms.OrderBy(r => r.Name).ToList();

            // Load bookings for selected date
            if (SelectedRoomId.HasValue)
            {
                var roomBookings = await _roomService.GetRoomBookingsAsync(SelectedRoomId.Value, SelectedDate);
                Bookings = roomBookings.ToList();
            }
            else
            {
                // Get all meetings for the selected date
                var allMeetings = await _meetingService.GetMeetingsByDateRangeAsync(SelectedDate, SelectedDate);
                Bookings = allMeetings.Where(m => m.MeetingRoomId.HasValue).ToList();
            }
            
            _logger.LogInformation("Retrieved {Count} bookings for date {Date}", Bookings.Count, SelectedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading room calendar");
            TempData["ErrorMessage"] = "An error occurred while loading the calendar.";
        }
    }
}
