using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace KickOffAPI.Services
{
    /// <summary>
    /// Generic caching service for distributed caching
    /// </summary>
    public class CacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

    /// <summary>
    /// Get value from cache
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    /// <summary>
    /// Set value in cache with default 60 minute expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await SetAsync(key, value, TimeSpan.FromMinutes(60), cancellationToken);
    }

    /// <summary>
    /// Set value in cache with custom expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    /// <summary>
    /// Remove value from cache
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Generate cache key from parameters
    /// </summary>
        public static string GenerateKey(string prefix, params object[] values)
        {
            return $"{prefix}:{string.Join(":", values)}";
        }
    }
}
