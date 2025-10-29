namespace MeetingManagementSystem.Core.DTOs;

public class MeetingAttendanceReportDto
{
    public int MeetingId { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public int TotalParticipants { get; set; }
    public int AcceptedCount { get; set; }
    public int DeclinedCount { get; set; }
    public int PendingCount { get; set; }
    public int AttendedCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class RoomUtilizationReportDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TotalBookings { get; set; }
    public int CompletedMeetings { get; set; }
    public int CancelledMeetings { get; set; }
    public double UtilizationRate { get; set; }
    public int TotalHoursBooked { get; set; }
    public double AverageOccupancy { get; set; }
}

public class MeetingStatisticsDto
{
    public int TotalMeetings { get; set; }
    public int ScheduledMeetings { get; set; }
    public int CompletedMeetings { get; set; }
    public int CancelledMeetings { get; set; }
    public int InProgressMeetings { get; set; }
    public double AverageDurationMinutes { get; set; }
    public double AverageParticipants { get; set; }
    public double CompletionRate { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalDocuments { get; set; }
    public Dictionary<string, int> MeetingsByDepartment { get; set; } = new();
    public Dictionary<string, int> MeetingsByStatus { get; set; } = new();
}

public class UserActivityReportDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int MeetingsOrganized { get; set; }
    public int MeetingsAttended { get; set; }
    public int ActionItemsAssigned { get; set; }
    public int ActionItemsCompleted { get; set; }
    public int DocumentsUploaded { get; set; }
    public double AttendanceRate { get; set; }
}
