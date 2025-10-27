using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class MeetingRepository : Repository<Meeting>, IMeetingRepository
{
    public MeetingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(m => m.Organizer)
            .Include(m => m.MeetingRoom)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Where(m => m.ScheduledDate >= startDate && m.ScheduledDate <= endDate)
            .OrderBy(m => m.ScheduledDate)
            .ThenBy(m => m.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByOrganizerAsync(int organizerId)
    {
        return await _dbSet
            .Include(m => m.MeetingRoom)
            .Include(m => m.Participants)
            .Where(m => m.OrganizerId == organizerId)
            .OrderByDescending(m => m.ScheduledDate)
            .ThenByDescending(m => m.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByParticipantAsync(int participantId)
    {
        return await _dbSet
            .Include(m => m.Organizer)
            .Include(m => m.MeetingRoom)
            .Include(m => m.Participants)
            .Where(m => m.Participants.Any(p => p.UserId == participantId))
            .OrderByDescending(m => m.ScheduledDate)
            .ThenByDescending(m => m.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByStatusAsync(MeetingStatus status)
    {
        return await _dbSet
            .Include(m => m.Organizer)
            .Include(m => m.MeetingRoom)
            .Where(m => m.Status == status)
            .OrderBy(m => m.ScheduledDate)
            .ThenBy(m => m.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByRoomAsync(int roomId, DateTime date)
    {
        return await _dbSet
            .Include(m => m.Organizer)
            .Where(m => m.MeetingRoomId == roomId && m.ScheduledDate.Date == date.Date)
            .OrderBy(m => m.StartTime)
            .ToListAsync();
    }

    public async Task<Meeting?> GetMeetingWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(m => m.Organizer)
            .Include(m => m.MeetingRoom)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Include(m => m.AgendaItems)
                .ThenInclude(a => a.Presenter)
            .Include(m => m.AgendaItems)
                .ThenInclude(a => a.ActionItems)
                    .ThenInclude(ai => ai.AssignedTo)
            .Include(m => m.Documents)
            .Include(m => m.Minutes)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> HasRoomConflictAsync(int? roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMeetingId = null)
    {
        if (!roomId.HasValue)
        {
            return false;
        }

        var query = _dbSet
            .Where(m => m.MeetingRoomId == roomId.Value
                && m.ScheduledDate.Date == date.Date
                && m.Status != MeetingStatus.Cancelled
                && (m.StartTime < endTime && m.EndTime > startTime));

        if (excludeMeetingId.HasValue)
        {
            query = query.Where(m => m.Id != excludeMeetingId.Value);
        }

        return await query.AnyAsync();
    }
}
