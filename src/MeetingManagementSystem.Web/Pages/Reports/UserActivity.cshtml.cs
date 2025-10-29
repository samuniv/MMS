using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Reports;

[Authorize]
public class UserActivityModel : PageModel
{
    private readonly IReportService _reportService;

    public UserActivityModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Now;

    [BindProperty(SupportsGet = true)]
    public string? Department { get; set; }

    public IEnumerable<UserActivityReportDto> UserActivityReport { get; set; } = new List<UserActivityReportDto>();

    public async Task OnGetAsync()
    {
        UserActivityReport = await _reportService.GetUserActivityReportAsync(StartDate, EndDate, Department);
    }
}
