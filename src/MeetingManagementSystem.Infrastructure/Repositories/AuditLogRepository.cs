using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task LogActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress)
    {
        var auditLog = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Changes = changes,
            IpAddress = ipAddress
        };

        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }
}
