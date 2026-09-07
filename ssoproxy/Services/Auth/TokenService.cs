using gd.Core;
using ssoproxy.Database.Redis;
using NLog;

namespace ssoproxy.Services.Auth;

/// <summary>
/// 負責處理 token 的驗證、儲存與移除邏輯，並與 Redis 資料庫進行互動。
/// </summary>
public class TokenService : ITokenService
{
    private readonly IRedisDatabase _redisDb;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

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
            var userInfo = await _redisDb.GetStringAsync(token);
            _logger.Info("[SSO_TOKEN_VALIDATION] Token validation result: {0}", userInfo);
            if (string.IsNullOrEmpty(userInfo))
            {
                throw new GDException(ssoproxy.Core.SsoProxyCode.TokenExpired);
            }

            return userInfo;
        }
        catch (Exception)
        {
            // 可依實際架構加上 Logger 紀錄
            throw new GDException(ssoproxy.Core.SsoProxyCode.RedisConnectionError);
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

    public string GenerateJweToken(params string[] idno)
    {
        // 生成一個新的 GUID 作為 Token
        return Guid.NewGuid().ToString("N");
    }

    private static string Base64UrlDecode(string value)
    {
        var padding = value.Length % 4;
        if (padding > 0)
        {
            value += new string('=', 4 - padding);
        }

        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/')));
    }

    private async void CheckJWEHeader(string token)
    {
        // 解析 JWE Header，檢查是否符合預期的加密演算法與簽名演算法
        // 若不符合，則拋出例外或記錄警告
        // 這裡僅示範，實際實作需依據 JWE 規範進行解析與驗證
        var parts = token.Split('.');
        if (parts.Length != 5)
        {
            throw new GDException(ssoproxy.Core.SsoProxyCode.InvalidToken);
        }

        var headerJson = Base64UrlDecode(parts[0]);
        // 解析 headerJson 並檢查 alg 與 enc 欄位
        // 若不符合預期，則拋出例外或記錄警告
    }
}
