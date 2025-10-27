namespace MeetingManagementSystem.Core.Exceptions;

public class RoomNotAvailableException : Exception
{
    public RoomNotAvailableException(int roomId, DateTime date, TimeSpan time)
        : base($"Room {roomId} is not available on {date:yyyy-MM-dd} at {time}.")
    {
    }
}
