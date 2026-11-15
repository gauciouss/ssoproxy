using Yarp.ReverseProxy.Configuration;

namespace ssoproxy.Proxy;

public sealed class DynamicProxyConfigProvider : IProxyConfigProvider
{
    private readonly InMemoryConfigProvider _provider = new(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());

    public IProxyConfig GetConfig() => _provider.GetConfig();

    public void Update(IEnumerable<RouteConfig> routes, IEnumerable<ClusterConfig> clusters)
    {
        _provider.Update(routes.ToList(), clusters.ToList());
    }
}
