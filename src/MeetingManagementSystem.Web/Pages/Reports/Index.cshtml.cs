using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Reports;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReportService _reportService;

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    public int TotalMeetingsThisMonth { get; set; }
    public double AverageAttendanceRate { get; set; }
    public double AverageRoomUtilization { get; set; }

    public async Task OnGetAsync()
    {
        var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var statistics = await _reportService.GetMeetingStatisticsAsync(startDate, endDate);
        TotalMeetingsThisMonth = statistics.TotalMeetings;

        var attendanceReport = await _reportService.GetMeetingAttendanceReportAsync(startDate, endDate);
        AverageAttendanceRate = attendanceReport.Any() ? attendanceReport.Average(a => a.AttendanceRate) : 0;

        var roomUtilization = await _reportService.GetRoomUtilizationReportAsync(startDate, endDate);
        AverageRoomUtilization = roomUtilization.Any() ? roomUtilization.Average(r => r.UtilizationRate) : 0;
    }
}
