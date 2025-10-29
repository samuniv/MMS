using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IAuditService
{
    Task LogCreateAsync<T>(T entity, int userId, string ipAddress) where T : class;
    Task LogUpdateAsync<T>(T oldEntity, T newEntity, int userId, string ipAddress) where T : class;
    Task LogDeleteAsync<T>(T entity, int userId, string ipAddress) where T : class;
    Task LogActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count);
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AuditLog>> GetFilteredLogsAsync(string? entityType = null, int? userId = null, DateTime? startDate = null, DateTime? endDate = null, string? action = null);
}
