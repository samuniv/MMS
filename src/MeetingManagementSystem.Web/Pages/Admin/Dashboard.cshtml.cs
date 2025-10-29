using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Constants;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.Admin;

[Authorize(Policy = Policies.AdministratorOnly)]
public class DashboardModel : PageModel
{
    private readonly ISystemMonitoringService _monitoringService;

    public DashboardModel(ISystemMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    public SystemHealthDto SystemHealth { get; set; } = new();
    public IEnumerable<AuditLogDto> RecentAuditLogs { get; set; } = new List<AuditLogDto>();

    public async Task OnGetAsync()
    {
        SystemHealth = await _monitoringService.GetSystemHealthAsync();
        RecentAuditLogs = await _monitoringService.GetRecentAuditLogsAsync(10);
    }
}
