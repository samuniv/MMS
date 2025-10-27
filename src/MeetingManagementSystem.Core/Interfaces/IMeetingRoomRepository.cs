using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IMeetingRoomRepository : IRepository<MeetingRoom>
{
    Task<IEnumerable<MeetingRoom>> GetActiveRoomsAsync();
    Task<IEnumerable<MeetingRoom>> GetAvailableRoomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<MeetingRoom?> GetRoomByNameAsync(string name);
}
