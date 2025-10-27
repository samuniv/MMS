using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Entities;

public class ScheduledReminder
{
    public int Id { get; set; }
    public ReminderType Type { get; set; }
    public int? MeetingId { get; set; }
    public int? ActionItemId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public Meeting? Meeting { get; set; }
    public ActionItem? ActionItem { get; set; }
}
