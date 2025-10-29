using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Reports;

[Authorize]
public class AttendanceModel : PageModel
{
    private readonly IReportService _reportService;

    public AttendanceModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Now;

    [BindProperty(SupportsGet = true)]
    public string? Department { get; set; }

    public IEnumerable<MeetingAttendanceReportDto> AttendanceReport { get; set; } = new List<MeetingAttendanceReportDto>();

    public async Task OnGetAsync()
    {
        AttendanceReport = await _reportService.GetMeetingAttendanceReportAsync(StartDate, EndDate, Department);
    }

    public async Task<IActionResult> OnGetExportPdfAsync()
    {
        var pdfBytes = await _reportService.ExportMeetingAttendanceReportToPdfAsync(StartDate, EndDate, Department);
        return File(pdfBytes, "text/html", $"attendance-report-{DateTime.Now:yyyyMMdd}.html");
    }
}
