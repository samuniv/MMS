using Microsoft.Extensions.Logging;
using Moq;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class ActionItemServiceTests
{
    private readonly Mock<IActionItemRepository> _actionItemRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ActionItemService>> _loggerMock;
    private readonly ActionItemService _actionItemService;

    public ActionItemServiceTests()
    {
        _actionItemRepositoryMock = new Mock<IActionItemRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<ActionItemService>>();

        _actionItemService = new ActionItemService(
            _actionItemRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateActionItemAsync_WithValidData_CreatesActionItem()
    {
        // Arrange
        var dto = new CreateActionItemDto
        {
            AgendaItemId = 1,
            Description = "Test action item",
            AssignedToId = 2,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        var createdItem = new ActionItem
        {
            Id = 1,
            AgendaItemId = dto.AgendaItemId,
            Description = dto.Description,
            AssignedToId = dto.AssignedToId,
            DueDate = dto.DueDate,
            Status = ActionItemStatus.Pending
        };

        _actionItemRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ActionItem>()))
            .ReturnsAsync(createdItem);

        _actionItemRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdItem);

        _notificationServiceMock.Setup(n => n.SendActionItemAssignmentAsync(It.IsAny<ActionItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _actionItemService.CreateActionItemAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.AssignedToId, result.AssignedToId);
        Assert.Equal(ActionItemStatus.Pending, result.Status);
        _actionItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ActionItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateActionItemAsync_WithValidData_UpdatesActionItem()
    {
        // Arrange
        var actionItemId = 1;
        var dto = new UpdateActionItemDto
        {
            Description = "Updated description",
            Status = ActionItemStatus.InProgress
        };

        var existingItem = new ActionItem
        {
            Id = actionItemId,
            AgendaItemId = 1,
            Description = "Original description",
            AssignedToId = 2,
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = ActionItemStatus.Pending
        };

        _actionItemRepositoryMock.Setup(r => r.GetByIdAsync(actionItemId))
            .ReturnsAsync(existingItem);

        _actionItemRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ActionItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _actionItemService.UpdateActionItemAsync(actionItemId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Status, result.Status);
        _actionItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ActionItem>()), Times.Once);
    }

    [Fact]
    public async Task CompleteActionItemAsync_WithValidId_CompletesActionItem()
    {
        // Arrange
        var actionItemId = 1;
        var actionItem = new ActionItem
        {
            Id = actionItemId,
            AgendaItemId = 1,
            Description = "Test action item",
            AssignedToId = 2,
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = ActionItemStatus.InProgress
        };

        _actionItemRepositoryMock.Setup(r => r.GetByIdAsync(actionItemId))
            .ReturnsAsync(actionItem);

        _actionItemRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ActionItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _actionItemService.CompleteActionItemAsync(actionItemId);

        // Assert
        Assert.True(result);
        Assert.Equal(ActionItemStatus.Completed, actionItem.Status);
        Assert.NotNull(actionItem.CompletedAt);
        _actionItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ActionItem>()), Times.Once);
    }

    [Fact]
    public async Task GetActionItemsByUserIdAsync_ReturnsUserActionItems()
    {
        // Arrange
        var userId = 2;
        var actionItems = new List<ActionItem>
        {
            new ActionItem
            {
                Id = 1,
                AgendaItemId = 1,
                Description = "Action item 1",
                AssignedToId = userId,
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ActionItemStatus.Pending
            },
            new ActionItem
            {
                Id = 2,
                AgendaItemId = 1,
                Description = "Action item 2",
                AssignedToId = userId,
                DueDate = DateTime.UtcNow.AddDays(14),
                Status = ActionItemStatus.InProgress
            }
        };

        _actionItemRepositoryMock.Setup(r => r.GetByAssignedUserIdAsync(userId))
            .ReturnsAsync(actionItems);

        // Act
        var result = await _actionItemService.GetActionItemsByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, item => Assert.Equal(userId, item.AssignedToId));
    }

    [Fact]
    public async Task GetOverdueActionItemsAsync_ReturnsOverdueItems()
    {
        // Arrange
        var overdueItems = new List<ActionItem>
        {
            new ActionItem
            {
                Id = 1,
                AgendaItemId = 1,
                Description = "Overdue item",
                AssignedToId = 2,
                DueDate = DateTime.UtcNow.AddDays(-1),
                Status = ActionItemStatus.Pending
            }
        };

        _actionItemRepositoryMock.Setup(r => r.GetOverdueActionItemsAsync())
            .ReturnsAsync(overdueItems);

        // Act
        var result = await _actionItemService.GetOverdueActionItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.First().DueDate < DateTime.UtcNow);
    }

    [Fact]
    public async Task SendActionItemRemindersAsync_SendsRemindersForDueItems()
    {
        // Arrange
        var tomorrow = DateTime.UtcNow.AddDays(1).Date;
        var dueItems = new List<ActionItem>
        {
            new ActionItem
            {
                Id = 1,
                AgendaItemId = 1,
                Description = "Due tomorrow",
                AssignedToId = 2,
                DueDate = tomorrow,
                Status = ActionItemStatus.Pending,
                AssignedTo = new User { Id = 2, Email = "test@example.com" },
                AgendaItem = new AgendaItem
                {
                    Id = 1,
                    Title = "Test Agenda",
                    Meeting = new Meeting { Id = 1, Title = "Test Meeting" }
                }
            }
        };

        _actionItemRepositoryMock.Setup(r => r.GetDueActionItemsAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(dueItems);

        _notificationServiceMock.Setup(n => n.SendActionItemReminderAsync(It.IsAny<ActionItem>()))
            .Returns(Task.CompletedTask);

        // Act
        await _actionItemService.SendActionItemRemindersAsync();

        // Assert
        _notificationServiceMock.Verify(n => n.SendActionItemReminderAsync(It.IsAny<ActionItem>()), Times.Once);
    }

    [Fact]
    public async Task DeleteActionItemAsync_WithValidId_DeletesActionItem()
    {
        // Arrange
        var actionItemId = 1;
        var actionItem = new ActionItem
        {
            Id = actionItemId,
            AgendaItemId = 1,
            Description = "Test action item",
            AssignedToId = 2,
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = ActionItemStatus.Pending
        };

        _actionItemRepositoryMock.Setup(r => r.GetByIdAsync(actionItemId))
            .ReturnsAsync(actionItem);

        _actionItemRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<ActionItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _actionItemService.DeleteActionItemAsync(actionItemId);

        // Assert
        Assert.True(result);
        _actionItemRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<ActionItem>()), Times.Once);
    }
}
