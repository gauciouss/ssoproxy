using ssoproxy.Repository.Proxy;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace ssoproxy.Tests;

public class ReverseProxySqlConfigLoaderTests
{
    [Fact]
    public void BuildConfig_ShouldCreateMatchingRouteAndCluster()
    {
        var routes = new[]
        {
            new ReverseProxyRouteRecord("test1-route", "target-web1", 100, "/{**catch-all}", "test1.gd.com", true)
        };

        var destinationRecords = new[]
        {
            new ReverseProxyDestinationRecord("target-web1", "destination1", "http://sso-poc-target-web1/", true)
        };

        var config = ReverseProxySqlConfigLoader.BuildConfig(routes, destinationRecords);

        Assert.Single(config.Routes);
        Assert.Equal("test1-route", config.Routes[0].RouteId);
        Assert.Equal("target-web1", config.Routes[0].ClusterId);
        Assert.Equal("/{**catch-all}", config.Routes[0].Match.Path);

        var hosts = Assert.IsAssignableFrom<IEnumerable<string>>(config.Routes[0].Match.Hosts);
        Assert.Contains("test1.gd.com", hosts);

        Assert.Single(config.Clusters);
        Assert.Equal("target-web1", config.Clusters.First().Key);

        Assert.True(config.Clusters.TryGetValue("target-web1", out var cluster));
        Assert.NotNull(cluster);
        var clusterDestinations = cluster?.Destinations;
        Assert.NotNull(clusterDestinations);
        if (clusterDestinations is null)
        {
            return;
        }

        Assert.True(clusterDestinations.TryGetValue("destination1", out var destination));
        Assert.NotNull(destination);
        if (destination is null)
        {
            return;
        }

        Assert.Equal("http://sso-poc-target-web1/", destination.Address);
    }
}
