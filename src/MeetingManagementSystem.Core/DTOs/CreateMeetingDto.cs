namespace MeetingManagementSystem.Core.DTOs;

public class CreateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int OrganizerId { get; set; }
    public int? MeetingRoomId { get; set; }
    public List<int> ParticipantIds { get; set; } = new();
}
