using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;
using System.Text;

namespace MeetingManagementSystem.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MeetingAttendanceReportDto>> GetMeetingAttendanceReportAsync(
        DateTime startDate, DateTime endDate, string? department = null)
    {
        var query = _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Participants)
            .Where(m => m.ScheduledDate >= startDate && m.ScheduledDate <= endDate);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(m => m.Organizer.Department == department);
        }

        var meetings = await query.ToListAsync();

        return meetings.Select(m =>
        {
            var totalParticipants = m.Participants.Count;
            var acceptedCount = m.Participants.Count(p => p.AttendanceStatus == AttendanceStatus.Accepted);
            var declinedCount = m.Participants.Count(p => p.AttendanceStatus == AttendanceStatus.Declined);
            var pendingCount = m.Participants.Count(p => p.AttendanceStatus == AttendanceStatus.Pending);
            var attendedCount = m.Participants.Count(p => p.AttendanceStatus == AttendanceStatus.Attended);
            var absentCount = m.Participants.Count(p => p.AttendanceStatus == AttendanceStatus.Absent);

            return new MeetingAttendanceReportDto
            {
                MeetingId = m.Id,
                MeetingTitle = m.Title,
                ScheduledDate = m.ScheduledDate,
                TotalParticipants = totalParticipants,
                AcceptedCount = acceptedCount,
                DeclinedCount = declinedCount,
                PendingCount = pendingCount,
                AttendedCount = attendedCount,
                AbsentCount = absentCount,
                AttendanceRate = totalParticipants > 0 ? (double)attendedCount / totalParticipants * 100 : 0,
                OrganizerName = $"{m.Organizer.FirstName} {m.Organizer.LastName}",
                Department = m.Organizer.Department
            };
        }).ToList();
    }

    public async Task<IEnumerable<RoomUtilizationReportDto>> GetRoomUtilizationReportAsync(
        DateTime startDate, DateTime endDate)
    {
        var rooms = await _context.MeetingRooms
            .Include(r => r.Meetings.Where(m => m.ScheduledDate >= startDate && m.ScheduledDate <= endDate))
            .ThenInclude(m => m.Participants)
            .Where(r => r.IsActive)
            .ToListAsync();

        return rooms.Select(r =>
        {
            var totalBookings = r.Meetings.Count;
            var completedMeetings = r.Meetings.Count(m => m.Status == MeetingStatus.Completed);
            var cancelledMeetings = r.Meetings.Count(m => m.Status == MeetingStatus.Cancelled);
            var totalHoursBooked = r.Meetings.Sum(m => (m.EndTime - m.StartTime).TotalHours);
            var averageOccupancy = r.Meetings.Any() 
                ? r.Meetings.Average(m => m.Participants.Count) 
                : 0;

            // Calculate utilization rate based on working hours (8 hours per day)
            var totalDays = (endDate - startDate).Days + 1;
            var availableHours = totalDays * 8; // 8 working hours per day
            var utilizationRate = availableHours > 0 ? (totalHoursBooked / availableHours) * 100 : 0;

            return new RoomUtilizationReportDto
            {
                RoomId = r.Id,
                RoomName = r.Name,
                Location = r.Location,
                Capacity = r.Capacity,
                TotalBookings = totalBookings,
                CompletedMeetings = completedMeetings,
                CancelledMeetings = cancelledMeetings,
                UtilizationRate = utilizationRate,
                TotalHoursBooked = (int)totalHoursBooked,
                AverageOccupancy = averageOccupancy
            };
        }).ToList();
    }

    public async Task<MeetingStatisticsDto> GetMeetingStatisticsAsync(
        DateTime startDate, DateTime endDate, string? department = null)
    {
        var query = _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Participants)
            .Include(m => m.Documents)
            .Where(m => m.ScheduledDate >= startDate && m.ScheduledDate <= endDate);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(m => m.Organizer.Department == department);
        }

        var meetings = await query.ToListAsync();

        var totalMeetings = meetings.Count;
        var scheduledMeetings = meetings.Count(m => m.Status == MeetingStatus.Scheduled);
        var completedMeetings = meetings.Count(m => m.Status == MeetingStatus.Completed);
        var cancelledMeetings = meetings.Count(m => m.Status == MeetingStatus.Cancelled);
        var inProgressMeetings = meetings.Count(m => m.Status == MeetingStatus.InProgress);

        var averageDuration = meetings.Any() 
            ? meetings.Average(m => (m.EndTime - m.StartTime).TotalMinutes) 
            : 0;
        var averageParticipants = meetings.Any() 
            ? meetings.Average(m => m.Participants.Count) 
            : 0;
        var completionRate = totalMeetings > 0 
            ? (double)completedMeetings / totalMeetings * 100 
            : 0;

        var meetingsByDepartment = meetings
            .GroupBy(m => m.Organizer.Department)
            .ToDictionary(g => g.Key, g => g.Count());

        var meetingsByStatus = meetings
            .GroupBy(m => m.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new MeetingStatisticsDto
        {
            TotalMeetings = totalMeetings,
            ScheduledMeetings = scheduledMeetings,
            CompletedMeetings = completedMeetings,
            CancelledMeetings = cancelledMeetings,
            InProgressMeetings = inProgressMeetings,
            AverageDurationMinutes = averageDuration,
            AverageParticipants = averageParticipants,
            CompletionRate = completionRate,
            TotalParticipants = meetings.Sum(m => m.Participants.Count),
            TotalDocuments = meetings.Sum(m => m.Documents.Count),
            MeetingsByDepartment = meetingsByDepartment,
            MeetingsByStatus = meetingsByStatus
        };
    }

    public async Task<IEnumerable<UserActivityReportDto>> GetUserActivityReportAsync(
        DateTime startDate, DateTime endDate, string? department = null)
    {
        var query = _context.Users
            .Include(u => u.OrganizedMeetings.Where(m => m.ScheduledDate >= startDate && m.ScheduledDate <= endDate))
            .Include(u => u.MeetingParticipations.Where(mp => mp.Meeting.ScheduledDate >= startDate && mp.Meeting.ScheduledDate <= endDate))
            .ThenInclude(mp => mp.Meeting)
            .Include(u => u.AssignedActionItems.Where(ai => ai.CreatedAt >= startDate && ai.CreatedAt <= endDate))
            .Include(u => u.UploadedDocuments.Where(d => d.UploadedAt >= startDate && d.UploadedAt <= endDate))
            .Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(u => u.Department == department);
        }

        var users = await query.ToListAsync();

        return users.Select(u =>
        {
            var meetingsOrganized = u.OrganizedMeetings.Count;
            var meetingsAttended = u.MeetingParticipations.Count(mp => mp.AttendanceStatus == AttendanceStatus.Attended);
            var totalMeetingInvitations = u.MeetingParticipations.Count;
            var actionItemsAssigned = u.AssignedActionItems.Count;
            var actionItemsCompleted = u.AssignedActionItems.Count(ai => ai.Status == ActionItemStatus.Completed);
            var documentsUploaded = u.UploadedDocuments.Count;
            var attendanceRate = totalMeetingInvitations > 0 
                ? (double)meetingsAttended / totalMeetingInvitations * 100 
                : 0;

            return new UserActivityReportDto
            {
                UserId = u.Id,
                UserName = $"{u.FirstName} {u.LastName}",
                Department = u.Department,
                MeetingsOrganized = meetingsOrganized,
                MeetingsAttended = meetingsAttended,
                ActionItemsAssigned = actionItemsAssigned,
                ActionItemsCompleted = actionItemsCompleted,
                DocumentsUploaded = documentsUploaded,
                AttendanceRate = attendanceRate
            };
        }).Where(u => u.MeetingsOrganized > 0 || u.MeetingsAttended > 0).ToList();
    }

    public async Task<byte[]> ExportMeetingAttendanceReportToPdfAsync(
        DateTime startDate, DateTime endDate, string? department = null)
    {
        var report = await GetMeetingAttendanceReportAsync(startDate, endDate, department);
        
        // Simple HTML to PDF conversion (basic implementation)
        var html = GenerateAttendanceReportHtml(report, startDate, endDate, department);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> ExportRoomUtilizationReportToExcelAsync(
        DateTime startDate, DateTime endDate)
    {
        var report = await GetRoomUtilizationReportAsync(startDate, endDate);
        
        // Simple CSV format (can be opened in Excel)
        var csv = GenerateRoomUtilizationCsv(report, startDate, endDate);
        return Encoding.UTF8.GetBytes(csv);
    }

    public async Task<byte[]> ExportMeetingStatisticsToExcelAsync(
        DateTime startDate, DateTime endDate, string? department = null)
    {
        var statistics = await GetMeetingStatisticsAsync(startDate, endDate, department);
        
        // Simple CSV format (can be opened in Excel)
        var csv = GenerateMeetingStatisticsCsv(statistics, startDate, endDate, department);
        return Encoding.UTF8.GetBytes(csv);
    }

    private string GenerateAttendanceReportHtml(
        IEnumerable<MeetingAttendanceReportDto> report, 
        DateTime startDate, DateTime endDate, string? department)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><title>Meeting Attendance Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin-top:20px;}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:8px;text-align:left;}");
        sb.AppendLine("th{background-color:#4CAF50;color:white;}");
        sb.AppendLine("h1{color:#333;}</style></head><body>");
        sb.AppendLine($"<h1>Meeting Attendance Report</h1>");
        sb.AppendLine($"<p><strong>Period:</strong> {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>");
        if (!string.IsNullOrEmpty(department))
            sb.AppendLine($"<p><strong>Department:</strong> {department}</p>");
        
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Meeting</th><th>Date</th><th>Organizer</th><th>Total</th><th>Attended</th><th>Absent</th><th>Attendance Rate</th></tr>");
        
        foreach (var item in report)
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{item.MeetingTitle}</td>");
            sb.AppendLine($"<td>{item.ScheduledDate:yyyy-MM-dd}</td>");
            sb.AppendLine($"<td>{item.OrganizerName}</td>");
            sb.AppendLine($"<td>{item.TotalParticipants}</td>");
            sb.AppendLine($"<td>{item.AttendedCount}</td>");
            sb.AppendLine($"<td>{item.AbsentCount}</td>");
            sb.AppendLine($"<td>{item.AttendanceRate:F1}%</td>");
            sb.AppendLine($"</tr>");
        }
        
        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    private string GenerateRoomUtilizationCsv(
        IEnumerable<RoomUtilizationReportDto> report, 
        DateTime startDate, DateTime endDate)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Room Utilization Report - {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("Room Name,Location,Capacity,Total Bookings,Completed,Cancelled,Hours Booked,Utilization Rate,Avg Occupancy");
        
        foreach (var item in report)
        {
            sb.AppendLine($"{item.RoomName},{item.Location},{item.Capacity},{item.TotalBookings}," +
                         $"{item.CompletedMeetings},{item.CancelledMeetings},{item.TotalHoursBooked}," +
                         $"{item.UtilizationRate:F1}%,{item.AverageOccupancy:F1}");
        }
        
        return sb.ToString();
    }

    private string GenerateMeetingStatisticsCsv(
        MeetingStatisticsDto statistics, 
        DateTime startDate, DateTime endDate, string? department)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Meeting Statistics Report - {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(department))
            sb.AppendLine($"Department: {department}");
        sb.AppendLine();
        
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Meetings,{statistics.TotalMeetings}");
        sb.AppendLine($"Scheduled,{statistics.ScheduledMeetings}");
        sb.AppendLine($"Completed,{statistics.CompletedMeetings}");
        sb.AppendLine($"Cancelled,{statistics.CancelledMeetings}");
        sb.AppendLine($"In Progress,{statistics.InProgressMeetings}");
        sb.AppendLine($"Average Duration (minutes),{statistics.AverageDurationMinutes:F1}");
        sb.AppendLine($"Average Participants,{statistics.AverageParticipants:F1}");
        sb.AppendLine($"Completion Rate,{statistics.CompletionRate:F1}%");
        sb.AppendLine($"Total Participants,{statistics.TotalParticipants}");
        sb.AppendLine($"Total Documents,{statistics.TotalDocuments}");
        
        sb.AppendLine();
        sb.AppendLine("Meetings by Department");
        sb.AppendLine("Department,Count");
        foreach (var dept in statistics.MeetingsByDepartment)
        {
            sb.AppendLine($"{dept.Key},{dept.Value}");
        }
        
        return sb.ToString();
    }
}
