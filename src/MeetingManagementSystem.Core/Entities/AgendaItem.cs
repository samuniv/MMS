namespace MeetingManagementSystem.Core.Entities;

public class AgendaItem
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int AllocatedMinutes { get; set; }
    public int? PresenterId { get; set; }
    
    // Navigation Properties
    public Meeting Meeting { get; set; } = null!;
    public User? Presenter { get; set; }
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
}