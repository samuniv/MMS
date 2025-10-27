namespace MeetingManagementSystem.Core.Exceptions;

public class MeetingNotFoundException : Exception
{
    public MeetingNotFoundException(int meetingId) 
        : base($"Meeting with ID {meetingId} was not found.")
    {
    }
}
