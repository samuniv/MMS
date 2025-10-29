using MeetingManagementSystem.Core.DTOs;

namespace MeetingManagementSystem.Core.Interfaces;

public interface ISystemMonitoringService
{
    Task<SystemHealthDto> GetSystemHealthAsync();
    Task<IEnumerable<AuditLogDto>> GetRecentAuditLogsAsync(int count = 50);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AuditLogDto>> GetFilteredAuditLogsAsync(string? entityType = null, int? userId = null, DateTime? startDate = null, DateTime? endDate = null, string? action = null);
    Task LogUserActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress);
}
