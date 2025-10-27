using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IActionItemRepository : IRepository<ActionItem>
{
    Task<IEnumerable<ActionItem>> GetByAgendaItemIdAsync(int agendaItemId);
    Task<IEnumerable<ActionItem>> GetByAssignedUserIdAsync(int userId);
    Task<IEnumerable<ActionItem>> GetByStatusAsync(ActionItemStatus status);
    Task<IEnumerable<ActionItem>> GetDueActionItemsAsync(DateTime dueDate);
    Task<IEnumerable<ActionItem>> GetOverdueActionItemsAsync();
}
