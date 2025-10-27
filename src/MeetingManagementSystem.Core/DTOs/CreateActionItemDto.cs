namespace MeetingManagementSystem.Core.DTOs;

public class CreateActionItemDto
{
    public int AgendaItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AssignedToId { get; set; }
    public DateTime DueDate { get; set; }
}
