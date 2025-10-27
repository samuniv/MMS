using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class MeetingMinutesRepository : Repository<MeetingMinutes>, IMeetingMinutesRepository
{
    public MeetingMinutesRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MeetingMinutes?> GetByMeetingIdAsync(int meetingId)
    {
        return await _dbSet
            .Include(m => m.Meeting)
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.MeetingId == meetingId);
    }

    public async Task<IEnumerable<MeetingMinutes>> GetMinutesHistoryAsync(int meetingId)
    {
        return await _dbSet
            .Include(m => m.CreatedBy)
            .Where(m => m.MeetingId == meetingId)
            .OrderByDescending(m => m.LastModified)
            .ToListAsync();
    }
}
