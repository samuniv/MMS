namespace MeetingManagementSystem.Core.DTOs;

public class SystemHealthDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalMeetings { get; set; }
    public int UpcomingMeetings { get; set; }
    public int TotalMeetingRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int TotalDocuments { get; set; }
    public long TotalDocumentSizeMB { get; set; }
    public int PendingActionItems { get; set; }
    public int OverdueActionItems { get; set; }
    public DateTime LastBackupDate { get; set; }
    public string SystemStatus { get; set; } = "Healthy";
    public Dictionary<string, int> RecentActivity { get; set; } = new();
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Changes { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
