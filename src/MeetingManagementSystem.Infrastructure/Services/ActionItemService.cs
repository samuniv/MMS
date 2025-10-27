using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeetingManagementSystem.Infrastructure.Services;

public class ActionItemService : IActionItemService
{
    private readonly IActionItemRepository _actionItemRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ActionItemService> _logger;

    public ActionItemService(
        IActionItemRepository actionItemRepository,
        INotificationService notificationService,
        ILogger<ActionItemService> logger)
    {
        _actionItemRepository = actionItemRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ActionItem> CreateActionItemAsync(CreateActionItemDto dto)
    {
        var actionItem = new ActionItem
        {
            AgendaItemId = dto.AgendaItemId,
            Description = dto.Description,
            AssignedToId = dto.AssignedToId,
            DueDate = dto.DueDate,
            Status = ActionItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var createdItem = await _actionItemRepository.AddAsync(actionItem);
        _logger.LogInformation("Action item created and assigned to user {UserId}", dto.AssignedToId);

        // Send notification to assigned user
        var itemWithDetails = await _actionItemRepository.GetByIdAsync(createdItem.Id);
        if (itemWithDetails != null)
        {
            await _notificationService.SendActionItemAssignmentAsync(itemWithDetails);
        }

        return createdItem;
    }

    public async Task<ActionItem?> GetActionItemByIdAsync(int id)
    {
        return await _actionItemRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ActionItem>> GetActionItemsByAgendaItemIdAsync(int agendaItemId)
    {
        return await _actionItemRepository.GetByAgendaItemIdAsync(agendaItemId);
    }

    public async Task<IEnumerable<ActionItem>> GetActionItemsByUserIdAsync(int userId)
    {
        return await _actionItemRepository.GetByAssignedUserIdAsync(userId);
    }

    public async Task<IEnumerable<ActionItem>> GetActionItemsByStatusAsync(ActionItemStatus status)
    {
        return await _actionItemRepository.GetByStatusAsync(status);
    }

    public async Task<ActionItem> UpdateActionItemAsync(int id, UpdateActionItemDto dto)
    {
        var actionItem = await _actionItemRepository.GetByIdAsync(id);
        if (actionItem == null)
        {
            throw new ArgumentException($"Action item with ID {id} not found");
        }

        if (dto.Description != null)
        {
            actionItem.Description = dto.Description;
        }

        if (dto.AssignedToId.HasValue)
        {
            actionItem.AssignedToId = dto.AssignedToId.Value;
        }

        if (dto.DueDate.HasValue)
        {
            actionItem.DueDate = dto.DueDate.Value;
        }

        if (dto.Status.HasValue)
        {
            actionItem.Status = dto.Status.Value;
            if (dto.Status.Value == ActionItemStatus.Completed)
            {
                actionItem.CompletedAt = DateTime.UtcNow;
            }
        }

        await _actionItemRepository.UpdateAsync(actionItem);
        _logger.LogInformation("Action item {ActionItemId} updated", id);

        return actionItem;
    }

    public async Task<bool> CompleteActionItemAsync(int id)
    {
        var actionItem = await _actionItemRepository.GetByIdAsync(id);
        if (actionItem == null)
        {
            return false;
        }

        actionItem.Status = ActionItemStatus.Completed;
        actionItem.CompletedAt = DateTime.UtcNow;

        await _actionItemRepository.UpdateAsync(actionItem);
        _logger.LogInformation("Action item {ActionItemId} marked as completed", id);

        return true;
    }

    public async Task<bool> DeleteActionItemAsync(int id)
    {
        var actionItem = await _actionItemRepository.GetByIdAsync(id);
        if (actionItem == null)
        {
            return false;
        }

        await _actionItemRepository.DeleteAsync(actionItem);
        _logger.LogInformation("Action item {ActionItemId} deleted", id);

        return true;
    }

    public async Task<IEnumerable<ActionItem>> GetOverdueActionItemsAsync()
    {
        return await _actionItemRepository.GetOverdueActionItemsAsync();
    }

    public async Task SendActionItemRemindersAsync()
    {
        var tomorrow = DateTime.UtcNow.AddDays(1).Date;
        var dueItems = await _actionItemRepository.GetDueActionItemsAsync(tomorrow);

        foreach (var item in dueItems)
        {
            await _notificationService.SendActionItemReminderAsync(item);
            _logger.LogInformation("Reminder sent for action item {ActionItemId}", item.Id);
        }
    }
}
