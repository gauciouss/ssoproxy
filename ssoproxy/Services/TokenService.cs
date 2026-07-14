using ssoproxy.Database;

namespace ssoproxy.Services;

/// <summary>
/// 負責處理 token 的驗證、儲存與移除邏輯，並與 Redis 資料庫進行互動。
/// </summary>
public class TokenService : ITokenService
{
    private readonly IRedisDatabase _redisDb;

    public TokenService(IRedisDatabase redisDb)
    {
        _redisDb = redisDb;
    }

    public async Task<string?> ValidateTokenAsync(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            return await _redisDb.GetStringAsync(token);
        }
        catch (Exception)
        {
            // 可依實際架構加上 Logger 紀錄
            throw;
        }
    }

    public async Task AddTokenAsync(string token, string userJson, TimeSpan? expiry = null)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("token is required", nameof(token));
        if (userJson is null) throw new ArgumentNullException(nameof(userJson));

        await _redisDb.SetStringAsync(token, userJson, expiry);
    }

    public async Task<bool> RemoveTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        return await _redisDb.RemoveAsync(token);
    }
}
