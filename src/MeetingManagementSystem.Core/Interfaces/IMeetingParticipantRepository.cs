using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingParticipantRepository : IRepository<MeetingParticipant>
{
    Task<IEnumerable<MeetingParticipant>> GetParticipantsByMeetingAsync(int meetingId);
    Task<MeetingParticipant?> GetParticipantAsync(int meetingId, int userId);
    Task<IEnumerable<MeetingParticipant>> GetParticipantsByStatusAsync(int meetingId, AttendanceStatus status);
    Task<bool> IsUserParticipantAsync(int meetingId, int userId);
}
