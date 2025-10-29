using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MeetingManagementSystem.Infrastructure.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        void RemoveByPrefix(string prefix);
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lock = new();

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            try
            {
                return _cache.TryGetValue(key, out T? value) ? value : default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
                return default;
            }
        }

        public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var value = await factory();
                Set(key, value, expiration);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing cache factory for key: {Key}", key);
                throw;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
                    Size = 1 // For size-based eviction
                };

                cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    lock (_lock)
                    {
                        _cacheKeys.Remove(evictedKey.ToString() ?? string.Empty);
                    }
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", evictedKey, reason);
                });

                _cache.Set(key, value, cacheOptions);

                lock (_lock)
                {
                    _cacheKeys.Add(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
                lock (_lock)
                {
                    _cacheKeys.Remove(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public void RemoveByPrefix(string prefix)
        {
            try
            {
                List<string> keysToRemove;
                lock (_lock)
                {
                    keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
                }

                foreach (var key in keysToRemove)
                {
                    Remove(key);
                }

                _logger.LogInformation("Removed {Count} cache keys with prefix: {Prefix}", keysToRemove.Count, prefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache keys by prefix: {Prefix}", prefix);
            }
        }
    }

    // Cache key constants
    public static class CacheKeys
    {
        public const string MeetingRooms = "meeting_rooms";
        public const string ActiveUsers = "active_users";
        public const string UserRoles = "user_roles_{0}";
        public const string MeetingDetails = "meeting_{0}";
        public const string UpcomingMeetings = "upcoming_meetings_{0}";
        public const string RoomAvailability = "room_availability_{0}_{1}";
        public const string UserNotificationPreferences = "notification_prefs_{0}";
        public const string SystemStatistics = "system_statistics";
        
        public static string GetMeetingKey(int meetingId) => string.Format(MeetingDetails, meetingId);
        public static string GetUserRolesKey(int userId) => string.Format(UserRoles, userId);
        public static string GetUpcomingMeetingsKey(int userId) => string.Format(UpcomingMeetings, userId);
        public static string GetRoomAvailabilityKey(int roomId, DateTime date) => 
            string.Format(RoomAvailability, roomId, date.ToString("yyyyMMdd"));
        public static string GetNotificationPreferencesKey(int userId) => 
            string.Format(UserNotificationPreferences, userId);
    }
}
