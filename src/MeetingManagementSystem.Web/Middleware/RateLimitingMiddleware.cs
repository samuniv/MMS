using System.Collections.Concurrent;

namespace MeetingManagementSystem.Web.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RequestCounter> _requestCounts = new();
    private static readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
    private const int _maxRequestsPerWindow = 100;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        
        if (!IsRequestAllowed(clientId))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get IP from X-Forwarded-For header (if behind proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            return ips[0].Trim();
        }

        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRequestAllowed(string clientId)
    {
        var now = DateTime.UtcNow;
        
        var counter = _requestCounts.GetOrAdd(clientId, _ => new RequestCounter());
        
        lock (counter)
        {
            // Clean up old requests outside the time window
            counter.Requests.RemoveAll(r => now - r > _timeWindow);
            
            // Check if limit exceeded
            if (counter.Requests.Count >= _maxRequestsPerWindow)
            {
                return false;
            }
            
            // Add current request
            counter.Requests.Add(now);
            return true;
        }
    }

    private class RequestCounter
    {
        public List<DateTime> Requests { get; } = new();
    }

    // Cleanup task to remove old entries
    public static void StartCleanupTask()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                
                var now = DateTime.UtcNow;
                var keysToRemove = _requestCounts
                    .Where(kvp => !kvp.Value.Requests.Any(r => now - r <= _timeWindow))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _requestCounts.TryRemove(key, out _);
                }
            }
        });
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        RateLimitingMiddleware.StartCleanupTask();
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
