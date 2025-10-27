using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<MeetingRoom>> GetAllRoomsAsync();
    Task<IEnumerable<MeetingRoom>> GetActiveRoomsAsync();
    Task<MeetingRoom?> GetRoomByIdAsync(int id);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMeetingId = null);
    Task<IEnumerable<MeetingRoom>> GetAvailableRoomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<IEnumerable<RoomAvailabilityDto>> GetRoomAvailabilityDetailsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<IEnumerable<TimeSlot>> GetAlternativeTimeSlotsAsync(int roomId, DateTime date, TimeSpan desiredStartTime, TimeSpan desiredEndTime);
    Task<IEnumerable<Meeting>> GetRoomBookingsAsync(int roomId, DateTime date);
}
