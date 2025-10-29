using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Infrastructure.Services;

public class SystemMonitoringService : ISystemMonitoringService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogRepository _auditLogRepository;

    public SystemMonitoringService(ApplicationDbContext context, IAuditLogRepository auditLogRepository)
    {
        _context = context;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var totalMeetings = await _context.Meetings.CountAsync();
        var upcomingMeetings = await _context.Meetings
            .CountAsync(m => m.ScheduledDate >= DateTime.Today && m.Status == MeetingStatus.Scheduled);
        var totalRooms = await _context.MeetingRooms.CountAsync();
        var activeRooms = await _context.MeetingRooms.CountAsync(r => r.IsActive);
        var totalDocuments = await _context.MeetingDocuments.CountAsync();
        var totalDocumentSize = await _context.MeetingDocuments.SumAsync(d => d.FileSize);
        var pendingActionItems = await _context.ActionItems
            .CountAsync(a => a.Status == ActionItemStatus.Pending || a.Status == ActionItemStatus.InProgress);
        var overdueActionItems = await _context.ActionItems
            .CountAsync(a => a.DueDate < DateTime.Today && a.Status != ActionItemStatus.Completed);

        // Get recent activity (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var recentActivity = new Dictionary<string, int>
        {
            ["Meetings Created"] = await _context.Meetings.CountAsync(m => m.CreatedAt >= sevenDaysAgo),
            ["Documents Uploaded"] = await _context.MeetingDocuments.CountAsync(d => d.UploadedAt >= sevenDaysAgo),
            ["Action Items Created"] = await _context.ActionItems.CountAsync(a => a.CreatedAt >= sevenDaysAgo),
            ["Users Registered"] = await _context.Users.CountAsync(u => u.CreatedAt >= sevenDaysAgo)
        };

        return new SystemHealthDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalMeetings = totalMeetings,
            UpcomingMeetings = upcomingMeetings,
            TotalMeetingRooms = totalRooms,
            ActiveRooms = activeRooms,
            TotalDocuments = totalDocuments,
            TotalDocumentSizeMB = totalDocumentSize / (1024 * 1024),
            PendingActionItems = pendingActionItems,
            OverdueActionItems = overdueActionItems,
            LastBackupDate = DateTime.UtcNow.AddDays(-1), // Placeholder
            SystemStatus = overdueActionItems > 10 ? "Warning" : "Healthy",
            RecentActivity = recentActivity
        };
    }

    public async Task<IEnumerable<AuditLogDto>> GetRecentAuditLogsAsync(int count = 50)
    {
        var logs = await _auditLogRepository.GetRecentLogsAsync(count);
        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            UserName = $"{l.User.FirstName} {l.User.LastName}",
            Timestamp = l.Timestamp,
            Changes = l.Changes,
            IpAddress = l.IpAddress
        });
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var logs = await _auditLogRepository.GetLogsByUserAsync(userId, startDate, endDate);
        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            UserName = $"{l.User.FirstName} {l.User.LastName}",
            Timestamp = l.Timestamp,
            Changes = l.Changes,
            IpAddress = l.IpAddress
        });
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var logs = await _auditLogRepository.GetLogsByDateRangeAsync(startDate, endDate);
        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            UserName = $"{l.User.FirstName} {l.User.LastName}",
            Timestamp = l.Timestamp,
            Changes = l.Changes,
            IpAddress = l.IpAddress
        });
    }

    public async Task LogUserActionAsync(string action, string entityType, int entityId, int userId, string changes, string ipAddress)
    {
        await _auditLogRepository.LogActionAsync(action, entityType, entityId, userId, changes, ipAddress);
    }
}
