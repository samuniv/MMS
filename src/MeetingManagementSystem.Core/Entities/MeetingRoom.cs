namespace MeetingManagementSystem.Core.Entities;

public class MeetingRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Equipment { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}