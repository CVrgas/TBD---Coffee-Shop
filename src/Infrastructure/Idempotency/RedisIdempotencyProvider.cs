using System.Text.Json;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Infrastructure.Idempotency;

public class RedisIdempotencyProvider(IConnectionMultiplexer redis) : IIdempotencyProvider
{
    private const string ProcessingStatus = "PROCESSING";
    private readonly IDatabase _db = redis.GetDatabase();
    private static string BuildIdempotencyKey(string key) =>  $"idempotency_{key}";
    
    public async Task<bool> TryReserveAsync(string key, TimeSpan expiry) => 
        await _db.StringSetAsync(BuildIdempotencyKey(key), ProcessingStatus, expiry, When.NotExists);
    
    public async Task SaveResponseAsync(string key, object response, TimeSpan expiry)
    {
        var serialized = JsonSerializer.Serialize(response);
        await _db.StringSetAsync(BuildIdempotencyKey(key), serialized,expiry);
    }
    
    public async Task RemoveAsync(string key) =>
        await _db.KeyDeleteAsync(BuildIdempotencyKey(key));
    
    public async Task<string?> GetResponseAsync(string key) =>
        await _db.StringGetAsync(BuildIdempotencyKey(key));
    
}