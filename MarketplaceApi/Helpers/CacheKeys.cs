namespace MarketplaceApi.Helpers;

public static class CacheKeys
{
    public const string User = "User";

    public static string MakeCacheKey(string key, string addition) => $"{key}_{addition}";
    public static string MakeCacheKey(string key, params string[] additions) => $"{key}_{string.Join("_", additions)}";
}