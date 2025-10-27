using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingMinutesRepository : IRepository<MeetingMinutes>
{
    Task<MeetingMinutes?> GetByMeetingIdAsync(int meetingId);
    Task<IEnumerable<MeetingMinutes>> GetMinutesHistoryAsync(int meetingId);
}
