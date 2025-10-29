using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count);
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task LogActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress);
}
