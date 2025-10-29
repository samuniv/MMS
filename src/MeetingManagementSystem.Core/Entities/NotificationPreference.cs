namespace MeetingManagementSystem.Core.Entities;

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Notification Types
    public bool MeetingInvitations { get; set; } = true;
    public bool MeetingReminders { get; set; } = true;
    public bool MeetingUpdates { get; set; } = true;
    public bool MeetingCancellations { get; set; } = true;
    public bool ActionItemAssignments { get; set; } = true;
    public bool ActionItemReminders { get; set; } = true;
    public bool ActionItemUpdates { get; set; } = true;
    
    // Delivery Methods
    public bool EmailNotifications { get; set; } = true;
    public bool SystemNotifications { get; set; } = true;
    
    // Reminder Timing
    public bool Reminder24Hours { get; set; } = true;
    public bool Reminder1Hour { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public User User { get; set; } = null!;
}
