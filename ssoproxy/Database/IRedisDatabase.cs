namespace ssoproxy.Database;

public interface IRedisDatabase
{
    Task<string?> GetStringAsync(string key);

    // 設定字串值，若提供 expiry 將設定過期時間
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

    // 刪除鍵值，若刪除成功回傳 true
    Task<bool> RemoveAsync(string key);
}
