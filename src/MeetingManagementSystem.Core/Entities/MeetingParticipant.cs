using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Entities;

public class MeetingParticipant
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public int UserId { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Pending;
    public DateTime? ResponseDate { get; set; }
    
    // Navigation Properties
    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}