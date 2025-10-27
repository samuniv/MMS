using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class ActionItemRepository : Repository<ActionItem>, IActionItemRepository
{
    public ActionItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ActionItem>> GetByAgendaItemIdAsync(int agendaItemId)
    {
        return await _dbSet
            .Include(a => a.AssignedTo)
            .Include(a => a.AgendaItem)
            .Where(a => a.AgendaItemId == agendaItemId)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActionItem>> GetByAssignedUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(a => a.AgendaItem)
                .ThenInclude(ag => ag.Meeting)
            .Include(a => a.AssignedTo)
            .Where(a => a.AssignedToId == userId)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActionItem>> GetByStatusAsync(ActionItemStatus status)
    {
        return await _dbSet
            .Include(a => a.AssignedTo)
            .Include(a => a.AgendaItem)
                .ThenInclude(ag => ag.Meeting)
            .Where(a => a.Status == status)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActionItem>> GetDueActionItemsAsync(DateTime dueDate)
    {
        return await _dbSet
            .Include(a => a.AssignedTo)
            .Include(a => a.AgendaItem)
                .ThenInclude(ag => ag.Meeting)
            .Where(a => a.DueDate.Date == dueDate.Date && a.Status != ActionItemStatus.Completed)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActionItem>> GetOverdueActionItemsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Include(a => a.AssignedTo)
            .Include(a => a.AgendaItem)
                .ThenInclude(ag => ag.Meeting)
            .Where(a => a.DueDate.Date < today && a.Status != ActionItemStatus.Completed)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }
}
