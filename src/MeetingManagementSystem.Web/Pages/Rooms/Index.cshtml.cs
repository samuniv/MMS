using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Rooms;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRoomService _roomService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IRoomService roomService, ILogger<IndexModel> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public List<MeetingRoom> Rooms { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var rooms = await _roomService.GetActiveRoomsAsync();
            Rooms = rooms.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("Retrieved {Count} meeting rooms", Rooms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting rooms");
            TempData["ErrorMessage"] = "An error occurred while loading meeting rooms.";
        }
    }
}
