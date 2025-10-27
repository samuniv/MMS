using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class MeetingRoomRepository : Repository<MeetingRoom>, IMeetingRoomRepository
{
    public MeetingRoomRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MeetingRoom>> GetActiveRoomsAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<MeetingRoom>> GetAvailableRoomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var bookedRoomIds = await _context.Meetings
            .Where(m => m.MeetingRoomId.HasValue
                && m.ScheduledDate.Date == date.Date
                && m.Status != MeetingStatus.Cancelled
                && (m.StartTime < endTime && m.EndTime > startTime))
            .Select(m => m.MeetingRoomId!.Value)
            .Distinct()
            .ToListAsync();

        return await _dbSet
            .Where(r => r.IsActive && !bookedRoomIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<MeetingRoom?> GetRoomByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
    }
}
