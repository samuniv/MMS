using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingMinutesService
{
    Task<MeetingMinutes> CreateMinutesAsync(CreateMeetingMinutesDto dto);
    Task<MeetingMinutes?> GetMinutesByIdAsync(int id);
    Task<MeetingMinutes?> GetMinutesByMeetingIdAsync(int meetingId);
    Task<MeetingMinutes> UpdateMinutesAsync(int id, UpdateMeetingMinutesDto dto);
    Task<bool> DeleteMinutesAsync(int id);
    Task<byte[]> ExportMinutesToPdfAsync(int minutesId);
}
