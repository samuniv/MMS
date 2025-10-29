using MeetingManagementSystem.Core.DTOs;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IReportService
{
    Task<IEnumerable<MeetingAttendanceReportDto>> GetMeetingAttendanceReportAsync(DateTime startDate, DateTime endDate, string? department = null);
    Task<IEnumerable<RoomUtilizationReportDto>> GetRoomUtilizationReportAsync(DateTime startDate, DateTime endDate);
    Task<MeetingStatisticsDto> GetMeetingStatisticsAsync(DateTime startDate, DateTime endDate, string? department = null);
    Task<IEnumerable<UserActivityReportDto>> GetUserActivityReportAsync(DateTime startDate, DateTime endDate, string? department = null);
    Task<byte[]> ExportMeetingAttendanceReportToPdfAsync(DateTime startDate, DateTime endDate, string? department = null);
    Task<byte[]> ExportRoomUtilizationReportToExcelAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportMeetingStatisticsToExcelAsync(DateTime startDate, DateTime endDate, string? department = null);
}
