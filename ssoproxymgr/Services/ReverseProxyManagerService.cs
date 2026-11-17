using System.Net.Http.Headers;
using System.Net.Http.Json;
using ssoproxymgr.Models;
using ssoproxymgr.Repository.Proxy;

namespace ssoproxymgr.Services;

public sealed class ReverseProxyManagerService
{
    private readonly IConfiguration _configuration;
    private readonly IReverseProxyConfigDao _configDao;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public ReverseProxyManagerService(IConfiguration configuration, IReverseProxyConfigDao configDao, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _configDao = configDao;
        _httpClient = httpClientFactory.CreateClient();
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReverseProxyConfigPayload> LoadCurrentConfigAsync()
    {
        _logger.Trace("Loading config from SQL...");
        var payload = new ReverseProxyConfigPayload();

        payload.Clusters = (await _configDao.GetClustersAsync(true))
            .Select(x => new ReverseProxyClusterModel
            {
                ClusterId = x.ClusterId,
                ClusterName = x.ClusterName,
                IsEnabled = x.IsEnabled
            })
            .ToList();

        payload.Routes = (await _configDao.GetRoutesAsync(true))
            .Select(x => new ReverseProxyRouteModel
            {
                RouteName = x.RouteName,
                ClusterId = x.ClusterId,
                Order = x.Order,
                MatchPath = x.MatchPath,
                MatchHosts = x.MatchHosts,
                IsEnabled = x.IsEnabled
            })
            .ToList();

        payload.Destinations = (await _configDao.GetDestinationsAsync(includeDisabled: true))
            .Select(x => new ReverseProxyDestinationModel
            {
                ClusterId = x.ClusterId,
                DestinationName = x.DestinationName,
                Address = x.Address,
                IsEnabled = x.IsEnabled
            })
            .ToList();

        _logger.Info("Loaded config from SQL.");

        return payload;
    }

    public async Task SaveConfigAsync(ReverseProxyConfigPayload payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        foreach (var cluster in payload.Clusters)
        {
            await _configDao.SaveClusterAsync(new ReverseProxyClusterRecord(
                cluster.ClusterId,
                cluster.ClusterName,
                cluster.IsEnabled));
        }

        foreach (var destination in payload.Destinations)
        {
            await _configDao.SaveDestinationAsync(new ReverseProxyDestinationRecord(
                destination.ClusterId,
                destination.DestinationName,
                destination.Address,
                destination.IsEnabled));
        }

        foreach (var route in payload.Routes)
        {
            await _configDao.SaveRouteAsync(new ReverseProxyRouteRecord(
                route.RouteName,
                route.ClusterId,
                route.Order,
                route.MatchPath,
                route.MatchHosts,
                route.IsEnabled));
        }
    }

    public async Task PublishAsync(ReverseProxyConfigPayload payload)
    {
        await SaveConfigAsync(payload);

        var ssoproxyApiUrl = _configuration["SsoproxyApiBaseUrl"] ?? "http://localhost:5000";
        var proxyToken = _httpContextAccessor.HttpContext?.User?.FindFirst("SsoproxyAccessToken")?.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ssoproxyApiUrl.TrimEnd('/')}/api/Mgr/publish")
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(proxyToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", proxyToken);
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Publish to ssoproxy failed: {response.StatusCode} - {message}");
        }
    }
}
