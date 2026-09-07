namespace ssoproxy.Services.Auth;

public interface IPathValidatorService
{
    /// <summary>
    /// 檢查請求路徑是否屬於免驗證路徑（包括登入頁面、登入提交與設定的白名單正則表示式路徑）
    /// </summary>
    bool IsBypassable(string requestPath);
}
