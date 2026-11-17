using gd.Core;
using ssoproxy.Repository.Redis;
using NLog;
using gd.Util;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace ssoproxy.Services.Auth;

/// <summary>
/// 負責處理 token 的驗證、儲存與移除邏輯，並與 Redis 資料庫進行互動。
/// </summary>
public class TokenService : ITokenService
{
    private readonly IRedisDatabase _redisDb;

    /// <summary>
    ///  Token 的有效期限，單位為分鐘。此值可從 appsettings.json 中的 "TokenExpiryMinutes" 設定取得，若未設定則預設為 30 分鐘。
    ///  這個常數用於在產生 JWE Token 時設定其過期時間，並在驗證 Token 時檢查其是否已過期。若需要更改 Token 的有效期限，可以在 appsettings.json 中修改 "TokenExpiryMinutes" 的值，或直接修改此常數的值。
    ///  注意：若要在程式中動態調整 Token 的有效期限，建議將此常數改為從設定檔讀取，而不是硬編碼在程式中，以便於在不同環境下進行配置。
    /// </summary>
    private readonly int TokenExpiryMinutes = 30;

    private readonly string _jweSecret = "sso-development-jwe-secret-32-bytes"; // 預設的 JWE 秘密金鑰，應從設定檔讀取
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public TokenService(IRedisDatabase redisDb, IConfiguration configuration)
    {
        _redisDb = redisDb;
        // 從 appsettings.json 中讀取 TokenExpiryMinutes 的設定值，若未設定則使用預設值 30
        TokenExpiryMinutes = configuration.GetValue<int>("Jwe.TokenExpiryMinutes", 30);
        // 從 appsettings.json 中讀取 _jweSecret 的設定值，若未設定則使用預設值
        _jweSecret = configuration.GetValue<string>("Jwe.Secret", _jweSecret);
    }       

    public async Task<string?> ValidateTokenAsync(string? authHeader)
    {
        Profiler profiler = new Profiler();
        _logger.Trace(profiler.StartLog("Validating token. AuthHeader: {0}"), authHeader);
        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        _logger.Debug("[SSO_TOKEN_VALIDATION] Extracted token: {0}", token);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            if (!CheckJWEToken(token))
            {
                throw new GDException(ssoproxy.Core.SsoProxyCode.InvalidToken);
            }

            var userInfo = await _redisDb.GetStringAsync(token);
            _logger.Info("[SSO_TOKEN_VALIDATION] Token validation result: {0}", userInfo);
            if (string.IsNullOrEmpty(userInfo))
            {
                throw new GDException(ssoproxy.Core.SsoProxyCode.InvalidToken);
            }
            _logger.Info(profiler.StopLog("Token validation completed successfully. Token: {0}, userInfo: {1}"), token, userInfo);
            return userInfo;
        }
        catch (GDException)
        {
            throw;
        }
        catch (Exception)
        {
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
        var headerJson = JsonSerializer.Serialize(new { alg = "dir", enc = "A256GCM" });
        var payloadJson = JsonSerializer.Serialize(new
        {
            sub = idno[0],
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddMinutes(TokenExpiryMinutes).ToUnixTimeSeconds()
        });

        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_jweSecret));
        var iv = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(payloadJson);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintext, ciphertext, tag);

        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedEncryptedKey = string.Empty;
        var encodedIv = Base64UrlEncode(iv);
        var encodedCiphertext = Base64UrlEncode(ciphertext);
        var encodedTag = Base64UrlEncode(tag);

        return string.Join('.', encodedHeader, encodedEncryptedKey, encodedIv, encodedCiphertext, encodedTag);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Base64UrlDecode(string value)
    {
        var padding = value.Length % 4;
        if (padding > 0)
        {
            value += new string('=', 4 - padding);
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/')));
    }

    private static byte[] Base64UrlDecodeBytes(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized += new string('=', 4 - padding);
        }

        return Convert.FromBase64String(normalized);
    }

    public bool CheckJWEToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 5)
        {
            return false;
        }

        try
        {
            var headerJson = Base64UrlDecode(parts[0]);
            using var headerDoc = JsonDocument.Parse(headerJson);
            if (!headerDoc.RootElement.TryGetProperty("alg", out var algProperty) ||
                !headerDoc.RootElement.TryGetProperty("enc", out var encProperty))
            {
                return false;
            }

            var alg = algProperty.GetString();
            var enc = encProperty.GetString();
            if (!string.Equals(alg, "dir", StringComparison.Ordinal) ||
                !string.Equals(enc, "A256GCM", StringComparison.Ordinal))
            {
                return false;
            }

            var key = SHA256.HashData(Encoding.UTF8.GetBytes(_jweSecret));
            var iv = Base64UrlDecodeBytes(parts[2]);
            var ciphertext = Base64UrlDecodeBytes(parts[3]);
            var tag = Base64UrlDecodeBytes(parts[4]);

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(iv, ciphertext, tag, plaintext);

            var payloadJson = Encoding.UTF8.GetString(plaintext);
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            if (!payloadDoc.RootElement.TryGetProperty("sub", out var subProperty) ||
                !payloadDoc.RootElement.TryGetProperty("exp", out var expProperty))
            {
                return false;
            }

            var sub = subProperty.GetString();
            if (string.IsNullOrWhiteSpace(sub))
            {
                return false;
            }

            if (!long.TryParse(expProperty.GetRawText().Trim('"'), out var expUnix))
            {
                return false;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expUnix;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
