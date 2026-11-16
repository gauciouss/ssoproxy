using System.Data.Common;
using ssoproxymgr.Repository;

namespace ssoproxymgr.Repository.Proxy;

public sealed class ReverseProxyConfigDao : IReverseProxyConfigDao
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ReverseProxyConfigDao(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// 獲取集群設定
    /// </summary>
    /// <param name="includeDisabled"></param>
    /// <returns></returns>
    public Task<List<ReverseProxyClusterRecord>> GetClustersAsync(bool includeDisabled = false) =>
        QueryListAsync(
            """
            SELECT cluster_id, cluster_name, is_enabled
            FROM dbo.proxy_cluster
            WHERE (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY cluster_name, cluster_id;
            """,
            reader => new ReverseProxyClusterRecord(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetBoolean(2)),
            new { includeDisabled = includeDisabled ? 1 : 0 });

    public Task<List<ReverseProxyRouteRecord>> GetRoutesAsync(bool includeDisabled = false) =>
        QueryListAsync(
            """
            SELECT route_name, cluster_id, route_order, match_path, match_hosts, is_enabled
            FROM dbo.proxy_route
            WHERE (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY route_order, route_name;
            """,
            reader => new ReverseProxyRouteRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetBoolean(5)),
            new { includeDisabled = includeDisabled ? 1 : 0 });

    /// <summary>
    /// 獲取目標設定
    /// </summary>
    /// <param name="includeDisabled"></param>
    /// <returns></returns>
    public Task<List<ReverseProxyDestinationRecord>> GetDestinationsAsync(bool includeDisabled = false) =>
        QueryListAsync(
            """
            SELECT cluster_id, destination_name, destination_address, is_enabled
            FROM dbo.proxy_destination
            WHERE (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY cluster_id, destination_name;
            """,
            reader => new ReverseProxyDestinationRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetBoolean(3)),
            new { includeDisabled = includeDisabled ? 1 : 0 });

    public Task<int> SaveClusterAsync(ReverseProxyClusterRecord cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(cluster.ClusterId);

        return ExecuteAsync(
            """
            IF EXISTS (SELECT 1 FROM dbo.proxy_cluster WHERE cluster_id = @clusterId)
            BEGIN
                UPDATE dbo.proxy_cluster
                SET cluster_name = @clusterName,
                    is_enabled = @isEnabled,
                    updated_at = SYSUTCDATETIME()
                WHERE cluster_id = @clusterId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.proxy_cluster (cluster_id, cluster_name, is_enabled)
                VALUES (@clusterId, @clusterName, @isEnabled);
            END;
            """,
            new
            {
                clusterId = cluster.ClusterId,
                clusterName = (object?)cluster.ClusterName ?? DBNull.Value,
                isEnabled = cluster.IsEnabled ? 1 : 0
            });
    }

    /// <summary>
    /// 保存路由設定
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    public Task<int> SaveRouteAsync(ReverseProxyRouteRecord route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.RouteName);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ClusterId);

        return ExecuteAsync(
            """
            IF EXISTS (SELECT 1 FROM dbo.proxy_route WHERE route_name = @routeName)
            BEGIN
                UPDATE dbo.proxy_route
                SET cluster_id = @clusterId,
                    route_order = @routeOrder,
                    match_path = @matchPath,
                    match_hosts = @matchHosts,
                    is_enabled = @isEnabled,
                    updated_at = SYSUTCDATETIME()
                WHERE route_name = @routeName;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.proxy_route (route_name, cluster_id, route_order, match_path, match_hosts, is_enabled)
                VALUES (@routeName, @clusterId, @routeOrder, @matchPath, @matchHosts, @isEnabled);
            END;
            """,
            new
            {
                routeName = route.RouteName,
                clusterId = route.ClusterId,
                routeOrder = route.Order,
                matchPath = (object?)route.MatchPath ?? DBNull.Value,
                matchHosts = (object?)route.MatchHosts ?? DBNull.Value,
                isEnabled = route.IsEnabled ? 1 : 0
            });
    }

    /// <summary>
    /// 保存目標設定
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public Task<int> SaveDestinationAsync(ReverseProxyDestinationRecord destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.ClusterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.DestinationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.Address);

        return ExecuteAsync(
            """
            IF EXISTS (SELECT 1 FROM dbo.proxy_destination
                       WHERE cluster_id = @clusterId AND destination_name = @destinationName)
            BEGIN
                UPDATE dbo.proxy_destination
                SET destination_address = @destinationAddress,
                    is_enabled = @isEnabled,
                    updated_at = SYSUTCDATETIME()
                WHERE cluster_id = @clusterId AND destination_name = @destinationName;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.proxy_destination (cluster_id, destination_name, destination_address, is_enabled)
                VALUES (@clusterId, @destinationName, @destinationAddress, @isEnabled);
            END;
            """,
            new
            {
                clusterId = destination.ClusterId,
                destinationName = destination.DestinationName,
                destinationAddress = destination.Address,
                isEnabled = destination.IsEnabled ? 1 : 0
            });
    }

    /// <summary>
    /// 執行查詢並返回結果列表
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="projector"></param>
    /// <param name="parameters"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private async Task<List<T>> QueryListAsync<T>(string sql, Func<DbDataReader, T> projector, object parameters)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<T>();
        while (await reader.ReadAsync())
        {
            results.Add(projector(reader));
        }

        return results;
    }

    private async Task<int> ExecuteAsync(string sql, object parameters)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    private static void AddParameters(DbCommand command, object parameters)
    {
        foreach (var property in parameters.GetType().GetProperties())
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{property.Name}";
            parameter.Value = property.GetValue(parameters) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
