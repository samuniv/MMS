namespace MeetingManagementSystem.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Changes { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    
    // Navigation Properties
    public User User { get; set; } = null!;
}