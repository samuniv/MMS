using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class MeetingParticipantRepository : Repository<MeetingParticipant>, IMeetingParticipantRepository
{
    public MeetingParticipantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MeetingParticipant>> GetParticipantsByMeetingAsync(int meetingId)
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.MeetingId == meetingId)
            .OrderBy(p => p.User.FirstName)
            .ThenBy(p => p.User.LastName)
            .ToListAsync();
    }

    public async Task<MeetingParticipant?> GetParticipantAsync(int meetingId, int userId)
    {
        return await _dbSet
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);
    }

    public async Task<IEnumerable<MeetingParticipant>> GetParticipantsByStatusAsync(int meetingId, AttendanceStatus status)
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.MeetingId == meetingId && p.AttendanceStatus == status)
            .OrderBy(p => p.User.FirstName)
            .ThenBy(p => p.User.LastName)
            .ToListAsync();
    }

    public async Task<bool> IsUserParticipantAsync(int meetingId, int userId)
    {
        return await _dbSet.AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId);
    }
}
