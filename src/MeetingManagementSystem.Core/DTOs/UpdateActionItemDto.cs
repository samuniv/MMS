using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.DTOs;

public class UpdateActionItemDto
{
    public string? Description { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? DueDate { get; set; }
    public ActionItemStatus? Status { get; set; }
}
