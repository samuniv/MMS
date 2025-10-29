using System.Security.Claims;

namespace MeetingManagementSystem.Web.Services;

public class AuditContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    public string GetIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return "Unknown";

        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            return ips[0].Trim();
        }

        // Get direct connection IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
