namespace MeetingManagementSystem.Core.DTOs;

public class CreateMeetingMinutesDto
{
    public int MeetingId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int CreatedById { get; set; }
}
