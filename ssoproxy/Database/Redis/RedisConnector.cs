using StackExchange.Redis;

namespace ssoproxy.Database.Redis;

public static class RedisConnector
{
    public static IConnectionMultiplexer Connect(string? connectionString, int maxRetries = 5, int retryDelayMs = 5000)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Redis 連線字串未設定。");
        }

        int retryCount = 0;

        while (true)
        {
            try
            {
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"[SSO_CONNECT_RETRY] Redis connection failed; retry {retryCount}/{maxRetries}... {ex.Message}");
                if (retryCount >= maxRetries) throw;
                Thread.Sleep(retryDelayMs);
            }
        }
    }
}
