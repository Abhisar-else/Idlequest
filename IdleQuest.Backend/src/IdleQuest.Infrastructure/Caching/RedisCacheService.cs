using System.Text.Json;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace IdleQuest.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var json = await _cache.GetStringAsync(key, ct);
        return json is null ? null : JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
        where T : class
    {
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOpts), opts, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _cache.RemoveAsync(key, ct);
}
