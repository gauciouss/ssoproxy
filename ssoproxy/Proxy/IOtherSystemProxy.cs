namespace ssoproxy.Proxy;

public interface IOtherSystemProxy
{
    Task<bool> PingExternalSystemAsync();
}
