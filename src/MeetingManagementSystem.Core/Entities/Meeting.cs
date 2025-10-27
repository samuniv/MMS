using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Entities;

public class Meeting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int OrganizerId { get; set; }
    public int? MeetingRoomId { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public User Organizer { get; set; } = null!;
    public MeetingRoom? MeetingRoom { get; set; }
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
    public ICollection<MeetingDocument> Documents { get; set; } = new List<MeetingDocument>();
    public MeetingMinutes? Minutes { get; set; }
}