using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Reports;

[Authorize]
public class StatisticsModel : PageModel
{
    private readonly IReportService _reportService;

    public StatisticsModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Now;

    [BindProperty(SupportsGet = true)]
    public string? Department { get; set; }

    public MeetingStatisticsDto Statistics { get; set; } = new();

    public async Task OnGetAsync()
    {
        Statistics = await _reportService.GetMeetingStatisticsAsync(StartDate, EndDate, Department);
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var csvBytes = await _reportService.ExportMeetingStatisticsToExcelAsync(StartDate, EndDate, Department);
        return File(csvBytes, "text/csv", $"meeting-statistics-{DateTime.Now:yyyyMMdd}.csv");
    }
}
