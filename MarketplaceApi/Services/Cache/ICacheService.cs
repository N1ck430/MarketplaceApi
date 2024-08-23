namespace MarketplaceApi.Services.Cache;

public interface ICacheService
{
    Task<T?> GetAndSetCacheEntry<T>(string cacheKey, Func<Task<T?>> getActual);
    void RemoveCacheKey(string cacheKey);

    void ClearCache();
}