using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.DTOs;

public class RespondToInvitationDto
{
    public int MeetingId { get; set; }
    public int UserId { get; set; }
    public AttendanceStatus Response { get; set; }
    public string? Comment { get; set; }
}
