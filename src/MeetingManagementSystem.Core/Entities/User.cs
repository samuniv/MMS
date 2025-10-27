using Microsoft.AspNetCore.Identity;

namespace MeetingManagementSystem.Core.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public ICollection<Meeting> OrganizedMeetings { get; set; } = new List<Meeting>();
    public ICollection<MeetingParticipant> MeetingParticipations { get; set; } = new List<MeetingParticipant>();
    public ICollection<AgendaItem> PresentedAgendaItems { get; set; } = new List<AgendaItem>();
    public ICollection<ActionItem> AssignedActionItems { get; set; } = new List<ActionItem>();
    public ICollection<MeetingDocument> UploadedDocuments { get; set; } = new List<MeetingDocument>();
    public ICollection<MeetingMinutes> CreatedMinutes { get; set; } = new List<MeetingMinutes>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}