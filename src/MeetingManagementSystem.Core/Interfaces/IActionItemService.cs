using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IActionItemService
{
    Task<ActionItem> CreateActionItemAsync(CreateActionItemDto dto);
    Task<ActionItem?> GetActionItemByIdAsync(int id);
    Task<IEnumerable<ActionItem>> GetActionItemsByAgendaItemIdAsync(int agendaItemId);
    Task<IEnumerable<ActionItem>> GetActionItemsByUserIdAsync(int userId);
    Task<IEnumerable<ActionItem>> GetActionItemsByStatusAsync(ActionItemStatus status);
    Task<ActionItem> UpdateActionItemAsync(int id, UpdateActionItemDto dto);
    Task<bool> CompleteActionItemAsync(int id);
    Task<bool> DeleteActionItemAsync(int id);
    Task<IEnumerable<ActionItem>> GetOverdueActionItemsAsync();
    Task SendActionItemRemindersAsync();
}
