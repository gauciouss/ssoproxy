using ssoproxy.Models;

namespace ssoproxy.Services;

/// <summary>
/// 負責處理使用者登入驗證與 token 產生邏輯。
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 驗證使用者帳號密碼，並在驗證成功時產生 token。
    /// </summary>
    /// <param name="username">使用者帳號。</param>
    /// <param name="password">使用者密碼。</param>
    /// <returns>包含驗證結果與 token 的登入結果。</returns>
    Task<LoginResult> AuthenticateAsync(string username, string password);
}
