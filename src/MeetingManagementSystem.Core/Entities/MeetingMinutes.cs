namespace MeetingManagementSystem.Core.Entities;

public class MeetingMinutes
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public Meeting Meeting { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}