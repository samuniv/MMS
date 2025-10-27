using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingService
{
    Task<Meeting> CreateMeetingAsync(CreateMeetingDto dto);
    Task<Meeting?> GetMeetingByIdAsync(int id);
    Task<Meeting?> GetMeetingWithDetailsAsync(int id);
    Task<IEnumerable<Meeting>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Meeting>> GetUserMeetingsAsync(int userId);
    Task<Meeting> UpdateMeetingAsync(int id, UpdateMeetingDto dto);
    Task<bool> CancelMeetingAsync(int id, string reason);
    Task<bool> UpdateMeetingStatusAsync(int id, MeetingStatus status);
    Task<bool> AddParticipantAsync(int meetingId, int userId);
    Task<bool> RemoveParticipantAsync(int meetingId, int userId);
    Task<bool> UpdateParticipantStatusAsync(int meetingId, int userId, AttendanceStatus status);
    Task<IEnumerable<MeetingParticipant>> GetMeetingParticipantsAsync(int meetingId);
}
