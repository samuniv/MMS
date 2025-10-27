using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Entities;

public class ActionItem
{
    public int Id { get; set; }
    public int AgendaItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AssignedToId { get; set; }
    public DateTime DueDate { get; set; }
    public ActionItemStatus Status { get; set; } = ActionItemStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Navigation Properties
    public AgendaItem AgendaItem { get; set; } = null!;
    public User AssignedTo { get; set; } = null!;
}