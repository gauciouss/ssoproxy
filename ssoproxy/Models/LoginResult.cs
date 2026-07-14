
namespace ssoproxy.Models;

/// <summary>
/// 表示登入驗證結果與 token 相關資料。
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 30;

    public static implicit operator LoginResult(ResponseEntity v)
    {
        throw new NotImplementedException();
    }
}
