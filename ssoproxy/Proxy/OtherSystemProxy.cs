using System.Net.Http;

namespace ssoproxy.Proxy;

public class OtherSystemProxy : IOtherSystemProxy
{
    private readonly HttpClient _httpClient;

    public OtherSystemProxy(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> PingExternalSystemAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.example.com/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
