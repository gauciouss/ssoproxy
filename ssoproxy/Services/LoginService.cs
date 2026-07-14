using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NLog;
using ssoproxy.Exceptions;
using ssoproxy.Models;
using gd.Core;

namespace ssoproxy.Services;

/// <summary>
/// 使用者登入服務，負責帳號驗證與 token 產生。
/// </summary>
public class LoginService : ILoginService
{
    private const int TokenExpiryMinutes = 30;
    private const string DefaultJweSecret = "sso-development-jwe-secret-32-bytes";

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ITokenService _tokenService;
    private readonly string _jweSecret;

    public LoginService(ITokenService tokenService, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _jweSecret = configuration["Jwe:Secret"] ?? DefaultJweSecret;
    }

    public async Task<LoginResult> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new GDException(SsoproxyExCode.MissingCredentials);
        }

        // TODO: 實際環境請改成呼叫身份驗證服務或資料庫帳密驗證
        if (username != "admin" || password != "admin123")
        {
            throw new GDException(SsoproxyExCode.InvalidCredentials);
        }

        var token = GenerateJweToken(username);
        var userJson = JsonSerializer.Serialize(new { Username = username, Role = "Admin" });

        await _tokenService.AddTokenAsync(token, userJson, TimeSpan.FromMinutes(TokenExpiryMinutes));

        _logger.Info("[SSO_LOGIN_SUCCESS] 使用者 {Username} 成功登入並產生 JWE token", username);

        return new LoginResult
        {
            Success = true,
            Token = token,
            Username = username,
            ExpiresInMinutes = TokenExpiryMinutes
        };
    }

    private string GenerateJweToken(string username)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "dir", enc = "A256GCM" });
        var payloadJson = JsonSerializer.Serialize(new
        {
            sub = username,
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
}
