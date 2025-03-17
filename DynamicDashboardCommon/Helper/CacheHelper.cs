using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Helper
{
    public static class CacheHelper
    {
        private static readonly ConcurrentDictionary<string, object> _cache = new();
        private static readonly ConcurrentDictionary<string, DateTime> _expiryTimes = new();
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly TimeSpan _defaultDuration = TimeSpan.FromMinutes(30);
        private static Timer _cleanupTimer;

        // Statistics
        private static long _hits = 0;
        private static long _misses = 0;

        static CacheHelper()
        {
            // Set up timer to periodically clean up expired items
            _cleanupTimer = new Timer(CleanupExpiredItems, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        public static T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var item) &&
                _expiryTimes.TryGetValue(key, out var expiry) &&
                DateTime.UtcNow < expiry)
            {
                Interlocked.Increment(ref _hits);
                return (T)item;
            }

            Interlocked.Increment(ref _misses);
            return default;
        }

        public static async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null)
        {
            // Try to get from cache first
            if (_cache.TryGetValue(key, out var cachedItem) &&
                _expiryTimes.TryGetValue(key, out var expiry) &&
                DateTime.UtcNow < expiry)
            {
                Interlocked.Increment(ref _hits);
                return (T)cachedItem;
            }

            // Not in cache or expired, create new value
            await _lock.WaitAsync();
            try
            {
                // Check again in case another thread already added it
                if (_cache.TryGetValue(key, out cachedItem) &&
                    _expiryTimes.TryGetValue(key, out expiry) &&
                    DateTime.UtcNow < expiry)
                {
                    Interlocked.Increment(ref _hits);
                    return (T)cachedItem;
                }

                // Create new value
                Interlocked.Increment(ref _misses);
                T newValue = await factory();

                // Store in cache
                _cache[key] = newValue;
                _expiryTimes[key] = DateTime.UtcNow.Add(duration ?? _defaultDuration);

                return newValue;
            }
            finally
            {
                _lock.Release();
            }
        }

        public static async Task AddOrUpdateAsync<T>(string key, T value, TimeSpan? duration = null)
        {
            await _lock.WaitAsync();
            try
            {
                if (_cache.ContainsKey(key))
                {
                    await RemoveAsync(key);
                }
                //TODO : Removed Temp, Also to check if we need browser cache at all or we need to replace it
              //  _cache.TryAdd(key, value);


             //   _expiryTimes[key] = DateTime.UtcNow.Add(duration ?? _defaultDuration);
            }
            finally
            {
                _lock.Release();
            }
        }

        public static async Task RemoveAsync(string key)
        {
            await _lock.WaitAsync();
            try
            {
                _cache.TryRemove(key, out _);
                _expiryTimes.TryRemove(key, out _);
            }
            finally
            {
                _lock.Release();
            }
        }

        public static async Task ClearAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _cache.Clear();
                _expiryTimes.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        private static void CleanupExpiredItems(object state)
        {
            // Avoid taking the lock in the timer to prevent potential deadlocks
            Task.Run(async () =>
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _expiryTimes
                    .Where(kvp => kvp.Value < now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (expiredKeys.Count > 0)
                {
                    await _lock.WaitAsync();
                    try
                    {
                        foreach (var key in expiredKeys)
                        {
                            _cache.TryRemove(key, out _);
                            _expiryTimes.TryRemove(key, out _);
                        }
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            });
        }


    }
}