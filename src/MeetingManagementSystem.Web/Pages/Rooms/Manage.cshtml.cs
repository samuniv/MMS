using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Web.Pages.Rooms;

[Authorize(Roles = "Administrator")]
public class ManageModel : PageModel
{
    private readonly IRoomService _roomService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ManageModel> _logger;

    public ManageModel(
        IRoomService roomService,
        ApplicationDbContext context,
        ILogger<ManageModel> logger)
    {
        _roomService = roomService;
        _context = context;
        _logger = logger;
    }

    public List<MeetingRoom> Rooms { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            Rooms = rooms.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("Retrieved {Count} meeting rooms for management", Rooms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting rooms");
            TempData["ErrorMessage"] = "An error occurred while loading meeting rooms.";
        }
    }

    public async Task<IActionResult> OnPostAddAsync(string name, string location, int capacity, string? equipment)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location) || capacity < 1)
            {
                TempData["ErrorMessage"] = "Please provide valid room details.";
                return RedirectToPage();
            }

            var room = new MeetingRoom
            {
                Name = name.Trim(),
                Location = location.Trim(),
                Capacity = capacity,
                Equipment = equipment?.Trim() ?? string.Empty,
                IsActive = true
            };

            _context.MeetingRooms.Add(room);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting room {RoomName} created", room.Name);
            TempData["SuccessMessage"] = "Meeting room added successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding meeting room");
            TempData["ErrorMessage"] = "An error occurred while adding the room.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, string location, int capacity, string? equipment)
    {
        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location) || capacity < 1)
            {
                TempData["ErrorMessage"] = "Please provide valid room details.";
                return RedirectToPage();
            }

            room.Name = name.Trim();
            room.Location = location.Trim();
            room.Capacity = capacity;
            room.Equipment = equipment?.Trim() ?? string.Empty;

            _context.MeetingRooms.Update(room);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting room {RoomId} updated", id);
            TempData["SuccessMessage"] = "Meeting room updated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meeting room {RoomId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the room.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int id)
    {
        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToPage();
            }

            room.IsActive = !room.IsActive;
            _context.MeetingRooms.Update(room);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting room {RoomId} status toggled to {Status}", id, room.IsActive);
            TempData["SuccessMessage"] = $"Room {(room.IsActive ? "activated" : "deactivated")} successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling room status {RoomId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the room status.";
        }

        return RedirectToPage();
    }
}
