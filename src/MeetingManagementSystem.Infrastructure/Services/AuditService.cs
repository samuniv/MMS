using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogCreateAsync<T>(T entity, int userId, string ipAddress) where T : class
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);
        var changes = SerializeEntity(entity);

        await _auditLogRepository.LogActionAsync(
            "Create",
            entityType,
            entityId,
            userId,
            changes,
            ipAddress
        );
    }

    public async Task LogUpdateAsync<T>(T oldEntity, T newEntity, int userId, string ipAddress) where T : class
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(newEntity);
        var changes = GetChanges(oldEntity, newEntity);

        await _auditLogRepository.LogActionAsync(
            "Update",
            entityType,
            entityId,
            userId,
            changes,
            ipAddress
        );
    }

    public async Task LogDeleteAsync<T>(T entity, int userId, string ipAddress) where T : class
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);
        var changes = SerializeEntity(entity);

        await _auditLogRepository.LogActionAsync(
            "Delete",
            entityType,
            entityId,
            userId,
            changes,
            ipAddress
        );
    }

    public async Task LogActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress)
    {
        await _auditLogRepository.LogActionAsync(action, entityType, entityId, userId, changes, ipAddress);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count)
    {
        return await _auditLogRepository.GetRecentLogsAsync(count);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _auditLogRepository.GetLogsByUserAsync(userId, startDate, endDate);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId)
    {
        return await _auditLogRepository.GetLogsByEntityAsync(entityType, entityId);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _auditLogRepository.GetLogsByDateRangeAsync(startDate, endDate);
    }

    public async Task<IEnumerable<AuditLog>> GetFilteredLogsAsync(
        string? entityType = null,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? action = null)
    {
        var query = await _auditLogRepository.GetAllAsync();
        var filteredQuery = query.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            filteredQuery = filteredQuery.Where(a => a.EntityType == entityType);

        if (userId.HasValue)
            filteredQuery = filteredQuery.Where(a => a.UserId == userId.Value);

        if (startDate.HasValue)
            filteredQuery = filteredQuery.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            filteredQuery = filteredQuery.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(action))
            filteredQuery = filteredQuery.Where(a => a.Action == action);

        return filteredQuery.OrderByDescending(a => a.Timestamp).ToList();
    }

    private int GetEntityId<T>(T entity) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            if (value != null)
            {
                return Convert.ToInt32(value);
            }
        }
        return 0;
    }

    private string SerializeEntity<T>(T entity) where T : class
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
        return JsonSerializer.Serialize(entity, options);
    }

    private string GetChanges<T>(T oldEntity, T newEntity) where T : class
    {
        var changes = new Dictionary<string, object>();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            // Skip navigation properties and collections
            if (property.PropertyType.IsClass && 
                property.PropertyType != typeof(string) && 
                !property.PropertyType.IsValueType)
                continue;

            var oldValue = property.GetValue(oldEntity);
            var newValue = property.GetValue(newEntity);

            if (!Equals(oldValue, newValue))
            {
                changes[property.Name] = new
                {
                    Old = oldValue,
                    New = newValue
                };
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };
        return JsonSerializer.Serialize(changes, options);
    }
}
