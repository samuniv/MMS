using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.DTOs;

public class UpdateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? MeetingRoomId { get; set; }
    public MeetingStatus Status { get; set; }
    public List<int> ParticipantIds { get; set; } = new();
}
