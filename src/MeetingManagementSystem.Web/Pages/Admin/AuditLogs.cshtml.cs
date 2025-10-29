using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Constants;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Admin;

[Authorize(Policy = Policies.AdministratorOnly)]
public class AuditLogsModel : PageModel
{
    private readonly ISystemMonitoringService _monitoringService;

    public AuditLogsModel(ISystemMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    public IEnumerable<AuditLogDto> AuditLogs { get; set; } = new List<AuditLogDto>();

    public async Task OnGetAsync()
    {
        // Use filtered logs if any filter is applied
        if (UserId.HasValue || !string.IsNullOrEmpty(EntityType) || !string.IsNullOrEmpty(Action) || StartDate.HasValue || EndDate.HasValue)
        {
            AuditLogs = await _monitoringService.GetFilteredAuditLogsAsync(EntityType, UserId, StartDate, EndDate, Action);
        }
        else
        {
            AuditLogs = await _monitoringService.GetRecentAuditLogsAsync(100);
        }
    }
}
