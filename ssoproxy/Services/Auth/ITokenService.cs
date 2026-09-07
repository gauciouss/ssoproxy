namespace ssoproxy.Services.Auth;

public interface ITokenService
{
    Task<string?> ValidateTokenAsync(string? authHeader);

    // 在 Redis 儲存 token 對應的使用者資訊，expiry 可選
    Task AddTokenAsync(string token, string userJson, TimeSpan? expiry = null);

    // 從 Redis 移除 token
    Task<bool> RemoveTokenAsync(string token);

    // 產生 JWE token
    string GenerateJweToken(params string[] idno);


}
