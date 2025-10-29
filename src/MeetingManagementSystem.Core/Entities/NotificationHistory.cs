namespace MeetingManagementSystem.Core.Entities;

public class NotificationHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty; // Email, System
    public bool IsDelivered { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RelatedMeetingId { get; set; }
    public int? RelatedActionItemId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
    public Meeting? RelatedMeeting { get; set; }
    public ActionItem? RelatedActionItem { get; set; }
}
