using StackExchange.Redis;

namespace ssoproxy.Database;

public class RedisDatabase : IRedisDatabase
{
    private readonly IDatabase _db;

    public RedisDatabase(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<string?> GetStringAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        if (expiry.HasValue)
        {
            await _db.StringSetAsync(key, value, expiry.Value);
        }
        else
        {
            await _db.StringSetAsync(key, value);
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }
}
