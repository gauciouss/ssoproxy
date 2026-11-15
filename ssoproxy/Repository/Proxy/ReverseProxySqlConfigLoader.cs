using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Yarp.ReverseProxy.Configuration;

namespace ssoproxy.Repository.Proxy;

public sealed record ReverseProxyClusterRecord(
    string ClusterId,
    string? ClusterName,
    bool IsEnabled);

public sealed record ReverseProxyRouteRecord(
    string RouteName,
    string ClusterId,
    int Order,
    string? MatchPath,
    string? MatchHosts,
    bool IsEnabled);

public sealed record ReverseProxyDestinationRecord(
    string ClusterId,
    string DestinationName,
    string Address,
    bool IsEnabled);

public static class ReverseProxySqlConfigLoader
{
    public static ReverseProxyRuntimeConfig BuildConfig(
        IEnumerable<ReverseProxyRouteRecord> routes,
        IEnumerable<ReverseProxyDestinationRecord> destinations)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(destinations);

        var destinationGroups = destinations
            .Where(x => x.IsEnabled)
            .GroupBy(x => x.ClusterId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => new ClusterConfig
                {
                    ClusterId = x.Key,
                    Destinations = x.ToDictionary(
                        d => d.DestinationName,
                        d => new DestinationConfig
                        {
                            Address = d.Address
                        },
                        StringComparer.OrdinalIgnoreCase)
                },
                StringComparer.OrdinalIgnoreCase);

        var routeConfigs = routes
            .Where(x => x.IsEnabled)
            .Select(x => new RouteConfig
            {
                RouteId = x.RouteName,
                ClusterId = x.ClusterId,
                Order = x.Order,
                Match = new RouteMatch
                {
                    Hosts = ParseHosts(x.MatchHosts),
                    Path = string.IsNullOrWhiteSpace(x.MatchPath) ? null : x.MatchPath
                }
            })
            .ToList();

        return new ReverseProxyRuntimeConfig(routeConfigs, destinationGroups);
    }

    public static async Task<ReverseProxyRuntimeConfig> LoadFromDatabaseAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionFactory = new SqlConnectionFactory(configuration);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var routes = await LoadRoutesAsync(connection);
        var destinations = await LoadDestinationsAsync(connection);

        return BuildConfig(routes, destinations);
    }

    private static string[] ParseHosts(string? hosts)
    {
        if (string.IsNullOrWhiteSpace(hosts))
        {
            return Array.Empty<string>();
        }

        return hosts
            .Split([';','\n','\r',','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<List<ReverseProxyRouteRecord>> LoadRoutesAsync(DbConnection connection)
    {
        const string sql = @"
            SELECT route_name, cluster_id, route_order, match_path, match_hosts, is_enabled
            FROM dbo.proxy_route
            WHERE is_enabled = 1
            ORDER BY route_order, route_name;";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        var routes = new List<ReverseProxyRouteRecord>();

        while (await reader.ReadAsync())
        {
            routes.Add(new ReverseProxyRouteRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetBoolean(5)));
        }

        return routes;
    }

    private static async Task<List<ReverseProxyDestinationRecord>> LoadDestinationsAsync(DbConnection connection)
    {
        const string sql = @"
            SELECT cluster_id, destination_name, destination_address, is_enabled
            FROM dbo.proxy_destination
            WHERE is_enabled = 1
            ORDER BY cluster_id, destination_name;";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        var destinations = new List<ReverseProxyDestinationRecord>();

        while (await reader.ReadAsync())
        {
            destinations.Add(new ReverseProxyDestinationRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetBoolean(3)));
        }

        return destinations;
    }
}

public sealed class ReverseProxyRuntimeConfig
{
    public ReverseProxyRuntimeConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyDictionary<string, ClusterConfig> clusters)
    {
        Routes = routes;
        Clusters = clusters;
    }

    public IReadOnlyList<RouteConfig> Routes { get; }

    public IReadOnlyDictionary<string, ClusterConfig> Clusters { get; }
}
