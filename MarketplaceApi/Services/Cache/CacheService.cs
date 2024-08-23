using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace MarketplaceApi.Services.Cache;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _defaultCacheTime = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, bool> CacheKeys = new();

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetAndSetCacheEntry<T>(string cacheKey, Func<Task<T?>> getActual)
    {
        CacheKeys.GetOrAdd(cacheKey, true);

        var cacheEntry = _memoryCache.Get<T>(cacheKey);
        if (cacheEntry != null)
        {
            return cacheEntry;
        }

        var actualEntry = await getActual.Invoke();

        if (actualEntry is null)
        {
            return default;
        }

        _memoryCache.Set(cacheKey, actualEntry, _defaultCacheTime);

        return actualEntry;
    }

    public void RemoveCacheKey(string cacheKey)
    {
        _memoryCache.Remove(cacheKey);
    }

    public void ClearCache()
    {
        foreach (var (cacheKey, _) in CacheKeys)
        {
            _memoryCache.Remove(cacheKey);
        }
    }
}