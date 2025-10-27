using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.DTOs;

public class RoomAvailabilityDto
{
    public MeetingRoom Room { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public List<TimeSlot> ConflictingSlots { get; set; } = new();
}

public class TimeSlot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
}
