# Performance Optimization Guide

## Overview

This document outlines the performance optimizations implemented in the Meeting Management System and provides guidelines for maintaining optimal performance.

## Database Optimizations

### Connection Pooling

The application uses Npgsql connection pooling with the following configuration:

```csharp
MinPoolSize: 5
MaxPoolSize: 100
ConnectionIdleLifetime: 300 seconds (5 minutes)
ConnectionPruningInterval: 10 seconds
```

### Query Optimizations

#### 1. Indexes

The following indexes have been created for optimal query performance:

**Meeting Table:**
- `IX_Meeting_DateTime`: Composite index on (ScheduledDate, StartTime)
- `IX_Meeting_Status`: Index on Status field
- `IX_Meeting_Organizer`: Index on OrganizerId
- `IX_Meeting_Room`: Index on MeetingRoomId

**User Table:**
- `IX_User_Email`: Unique index on Email
- `IX_User_Department`: Index on Department
- `IX_User_IsActive`: Index on IsActive

**MeetingParticipant Table:**
- `IX_MeetingParticipant_Unique`: Unique composite index on (MeetingId, UserId)

**ActionItem Table:**
- `IX_ActionItem_DueDate`: Index on DueDate
- `IX_ActionItem_AssignedStatus`: Composite index on (AssignedToId, Status)

**AuditLog Table:**
- `IX_AuditLog_Timestamp`: Index on Timestamp
- `IX_AuditLog_Entity`: Composite index on (EntityType, EntityId)
- `IX_AuditLog_User`: Index on UserId

#### 2. Query Splitting

The application uses query splitting for better performance when loading entities with multiple collections:

```csharp
options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
```

#### 3. No-Tracking Queries

For read-only operations, use no-tracking queries:

```csharp
var meetings = await _context.Meetings
    .AsNoTracking()
    .Where(m => m.ScheduledDate >= DateTime.Today)
    .ToListAsync();
```

#### 4. Projection

Use projection to select only required fields:

```csharp
var meetingList = await _context.Meetings
    .Select(m => new MeetingListDto
    {
        Id = m.Id,
        Title = m.Title,
        ScheduledDate = m.ScheduledDate
    })
    .ToListAsync();
```

### Database Maintenance

#### Regular Maintenance Tasks

Run these commands periodically for optimal performance:

```sql
-- Vacuum and analyze (recommended: weekly)
VACUUM ANALYZE;

-- Reindex (recommended: monthly)
REINDEX DATABASE meetingmanagement;

-- Update statistics (recommended: daily)
ANALYZE;
```

#### Monitoring Queries

**Check database size:**
```sql
SELECT pg_size_pretty(pg_database_size('meetingmanagement'));
```

**Check table sizes:**
```sql
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

**Check slow queries:**
```sql
SELECT 
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
```

**Check active connections:**
```sql
SELECT 
    count(*),
    state
FROM pg_stat_activity
WHERE datname = 'meetingmanagement'
GROUP BY state;
```

## Caching Strategy

### Memory Cache

The application uses in-memory caching for frequently accessed data:

#### Cached Data

- **Meeting Rooms**: Cached for 30 minutes
- **Active Users**: Cached for 15 minutes
- **User Roles**: Cached for 1 hour
- **Meeting Details**: Cached for 10 minutes
- **Room Availability**: Cached for 5 minutes
- **System Statistics**: Cached for 5 minutes

#### Cache Invalidation

Cache is automatically invalidated when:
- Data is updated or deleted
- Cache expiration time is reached
- Manual cache clear is triggered

#### Usage Example

```csharp
// Get from cache or execute factory
var rooms = await _cacheService.GetAsync(
    CacheKeys.MeetingRooms,
    async () => await _context.MeetingRooms.ToListAsync(),
    TimeSpan.FromMinutes(30)
);

// Invalidate cache
_cacheService.RemoveByPrefix("meeting_");
```

## Response Caching

### Static Files

Static files are cached for 7 days:

```csharp
Cache-Control: public, max-age=604800
```

### Response Compression

The application uses Brotli and Gzip compression for responses:

- Brotli: Preferred for modern browsers
- Gzip: Fallback for older browsers

## Performance Monitoring

### Built-in Monitoring

The application includes a performance monitoring service that tracks:

- Operation execution times
- Slow query detection (> 1 second)
- Average, min, and max durations
- Total operation calls

### Usage

```csharp
using (var timer = _performanceMonitoring.MeasureOperation("GetMeetings"))
{
    var meetings = await _meetingService.GetMeetingsAsync();
}
```

### Metrics Endpoint

Access performance metrics at: `/Admin/Performance`

## Best Practices

### 1. Database Queries

- **Use AsNoTracking()** for read-only queries
- **Use projection** to select only needed fields
- **Avoid N+1 queries** by using Include() or explicit loading
- **Use pagination** for large result sets
- **Batch operations** when possible

### 2. Caching

- **Cache frequently accessed data** that doesn't change often
- **Set appropriate expiration times** based on data volatility
- **Invalidate cache** when data changes
- **Monitor cache hit rates** to optimize cache strategy

### 3. API Design

- **Use DTOs** instead of returning entities directly
- **Implement pagination** for list endpoints
- **Use async/await** for all I/O operations
- **Avoid blocking calls** in async methods

### 4. Resource Management

- **Dispose resources properly** using using statements
- **Limit concurrent operations** to prevent resource exhaustion
- **Use connection pooling** for database connections
- **Monitor memory usage** and adjust cache limits

## Performance Targets

### Response Times

- **Page Load**: < 2 seconds
- **API Calls**: < 500ms
- **Database Queries**: < 100ms
- **Cache Hits**: > 80%

### Resource Usage

- **CPU**: < 70% average
- **Memory**: < 80% of allocated
- **Database Connections**: < 50 active
- **Disk I/O**: < 80% utilization

## Troubleshooting

### Slow Queries

1. Check query execution plan:
```sql
EXPLAIN ANALYZE SELECT * FROM meetings WHERE scheduled_date >= CURRENT_DATE;
```

2. Verify indexes are being used:
```sql
SELECT * FROM pg_stat_user_indexes WHERE schemaname = 'public';
```

3. Check for missing indexes:
```sql
SELECT * FROM pg_stat_user_tables WHERE schemaname = 'public';
```

### High Memory Usage

1. Check cache size and adjust limits
2. Review query result set sizes
3. Implement pagination for large lists
4. Monitor for memory leaks

### Connection Pool Exhaustion

1. Check active connections:
```sql
SELECT count(*) FROM pg_stat_activity WHERE datname = 'meetingmanagement';
```

2. Verify connection pool settings
3. Check for connection leaks (not disposed properly)
4. Increase pool size if needed

### Slow Page Loads

1. Enable response compression
2. Optimize static file caching
3. Minimize database queries per page
4. Use async loading for non-critical content
5. Implement lazy loading for images

## Monitoring Tools

### Application Insights (Optional)

For production monitoring, consider integrating Application Insights:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### PostgreSQL Monitoring

Enable pg_stat_statements extension:

```sql
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
```

### Docker Stats

Monitor container resource usage:

```bash
docker stats meetingmanagement_app_prod meetingmanagement_postgres_prod
```

## Performance Testing

### Load Testing

Use tools like Apache JMeter or k6 for load testing:

```bash
# Example k6 test
k6 run --vus 10 --duration 30s load-test.js
```

### Database Benchmarking

Use pgbench for database performance testing:

```bash
pgbench -i -s 50 meetingmanagement
pgbench -c 10 -j 2 -t 1000 meetingmanagement
```

## Continuous Optimization

1. **Regular Reviews**: Review performance metrics weekly
2. **Query Analysis**: Analyze slow queries monthly
3. **Index Optimization**: Review and optimize indexes quarterly
4. **Cache Strategy**: Adjust cache settings based on usage patterns
5. **Load Testing**: Perform load tests before major releases
