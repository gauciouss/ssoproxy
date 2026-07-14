using gd.Core;

namespace ssoproxy.Exceptions;

/// <summary>
/// ssoproxy 專屬的錯誤代碼與訊息，繼承自 gd.Core 的 GDExCode。
/// </summary>
public class SsoproxyExCode : GDExCode
{
    public SsoproxyExCode(string code, string message)
        : base(code, message)
    {
    }

    public static readonly SsoproxyExCode MissingCredentials = new("SSO-001", "帳號與密碼不可為空");
    public static readonly SsoproxyExCode InvalidCredentials = new("SSO-002", "帳號或密碼錯誤");
    public static readonly SsoproxyExCode InvalidCaptcha = new("SSO-003", "圖形驗證碼驗證失敗");
    public static readonly SsoproxyExCode TokenInvalid = new("SSO-004", "Token 已過期或不存在");
}
