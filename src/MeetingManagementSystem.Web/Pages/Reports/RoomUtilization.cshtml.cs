using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Reports;

[Authorize]
public class RoomUtilizationModel : PageModel
{
    private readonly IReportService _reportService;

    public RoomUtilizationModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Now;

    public IEnumerable<RoomUtilizationReportDto> RoomUtilizationReport { get; set; } = new List<RoomUtilizationReportDto>();

    public async Task OnGetAsync()
    {
        RoomUtilizationReport = await _reportService.GetRoomUtilizationReportAsync(StartDate, EndDate);
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var csvBytes = await _reportService.ExportRoomUtilizationReportToExcelAsync(StartDate, EndDate);
        return File(csvBytes, "text/csv", $"room-utilization-{DateTime.Now:yyyyMMdd}.csv");
    }
}
