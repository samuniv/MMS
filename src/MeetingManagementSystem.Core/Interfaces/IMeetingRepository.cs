using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingRepository : IRepository<Meeting>
{
    Task<IEnumerable<Meeting>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Meeting>> GetMeetingsByOrganizerAsync(int organizerId);
    Task<IEnumerable<Meeting>> GetMeetingsByParticipantAsync(int participantId);
    Task<IEnumerable<Meeting>> GetMeetingsByStatusAsync(MeetingStatus status);
    Task<IEnumerable<Meeting>> GetMeetingsByRoomAsync(int roomId, DateTime date);
    Task<Meeting?> GetMeetingWithDetailsAsync(int id);
    Task<bool> HasRoomConflictAsync(int? roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMeetingId = null);
}
