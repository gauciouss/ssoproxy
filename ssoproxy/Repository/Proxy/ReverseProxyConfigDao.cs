using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ssoproxy.Repository.Proxy;

public sealed class ReverseProxyConfigDao : IReverseProxyConfigDao
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ReverseProxyConfigDao(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<List<ReverseProxyClusterRecord>> GetClustersAsync(bool includeDisabled = false)
    {
        const string sql = @"
            SELECT cluster_id, cluster_name, is_enabled
            FROM dbo.proxy_cluster
            WHERE (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY cluster_name, cluster_id;";

        return await QueryListAsync(sql, reader => new ReverseProxyClusterRecord(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetBoolean(2)),
            new { includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<ReverseProxyClusterRecord?> GetClusterAsync(string clusterId, bool includeDisabled = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        const string sql = @"
            SELECT TOP 1 cluster_id, cluster_name, is_enabled
            FROM dbo.proxy_cluster
            WHERE cluster_id = @clusterId
              AND (@includeDisabled = 1 OR is_enabled = 1);";

        return await QuerySingleAsync(sql, reader => new ReverseProxyClusterRecord(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetBoolean(2)),
            new { clusterId, includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<int> SaveClusterAsync(ReverseProxyClusterRecord cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(cluster.ClusterId);

        const string sql = @"
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
            END;";

        return await ExecuteAsync(sql, new
        {
            clusterId = cluster.ClusterId,
            clusterName = (object?)cluster.ClusterName ?? DBNull.Value,
            isEnabled = cluster.IsEnabled ? 1 : 0
        });
    }

    public async Task<int> DeleteClusterAsync(string clusterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using (var clearRoutes = connection.CreateCommand())
            {
                clearRoutes.Transaction = transaction;
                clearRoutes.CommandText = "DELETE FROM dbo.proxy_route WHERE cluster_id = @clusterId;";
                AddParameter(clearRoutes, "@clusterId", clusterId);
                await clearRoutes.ExecuteNonQueryAsync();
            }

            await using (var clearDestinations = connection.CreateCommand())
            {
                clearDestinations.Transaction = transaction;
                clearDestinations.CommandText = "DELETE FROM dbo.proxy_destination WHERE cluster_id = @clusterId;";
                AddParameter(clearDestinations, "@clusterId", clusterId);
                await clearDestinations.ExecuteNonQueryAsync();
            }

            await using (var deleteCluster = connection.CreateCommand())
            {
                deleteCluster.Transaction = transaction;
                deleteCluster.CommandText = "DELETE FROM dbo.proxy_cluster WHERE cluster_id = @clusterId;";
                AddParameter(deleteCluster, "@clusterId", clusterId);
                await deleteCluster.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return 1;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ReverseProxyRouteRecord>> GetRoutesAsync(bool includeDisabled = false)
    {
        const string sql = @"
            SELECT route_name, cluster_id, route_order, match_path, match_hosts, is_enabled
            FROM dbo.proxy_route
            WHERE (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY route_order, route_name;";

        return await QueryListAsync(sql, reader => new ReverseProxyRouteRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetBoolean(5)),
            new { includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<ReverseProxyRouteRecord?> GetRouteAsync(string routeName, bool includeDisabled = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        const string sql = @"
            SELECT TOP 1 route_name, cluster_id, route_order, match_path, match_hosts, is_enabled
            FROM dbo.proxy_route
            WHERE route_name = @routeName
              AND (@includeDisabled = 1 OR is_enabled = 1);";

        return await QuerySingleAsync(sql, reader => new ReverseProxyRouteRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetBoolean(5)),
            new { routeName, includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<int> SaveRouteAsync(ReverseProxyRouteRecord route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.RouteName);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ClusterId);

        const string sql = @"
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
            END;";

        return await ExecuteAsync(sql, new
        {
            routeName = route.RouteName,
            clusterId = route.ClusterId,
            routeOrder = route.Order,
            matchPath = (object?)route.MatchPath ?? DBNull.Value,
            matchHosts = (object?)route.MatchHosts ?? DBNull.Value,
            isEnabled = route.IsEnabled ? 1 : 0
        });
    }

    public async Task<int> DeleteRouteAsync(string routeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        const string sql = "DELETE FROM dbo.proxy_route WHERE route_name = @routeName;";
        return await ExecuteAsync(sql, new { routeName });
    }

    public async Task<List<ReverseProxyDestinationRecord>> GetDestinationsAsync(string? clusterId = null, bool includeDisabled = false)
    {
        const string sql = @"
            SELECT cluster_id, destination_name, destination_address, is_enabled
            FROM dbo.proxy_destination
            WHERE (@clusterId IS NULL OR cluster_id = @clusterId)
              AND (@includeDisabled = 1 OR is_enabled = 1)
            ORDER BY cluster_id, destination_name;";

        return await QueryListAsync(sql, reader => new ReverseProxyDestinationRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetBoolean(3)),
            new { clusterId, includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<ReverseProxyDestinationRecord?> GetDestinationAsync(string clusterId, string destinationName, bool includeDisabled = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationName);

        const string sql = @"
            SELECT TOP 1 cluster_id, destination_name, destination_address, is_enabled
            FROM dbo.proxy_destination
            WHERE cluster_id = @clusterId
              AND destination_name = @destinationName
              AND (@includeDisabled = 1 OR is_enabled = 1);";

        return await QuerySingleAsync(sql, reader => new ReverseProxyDestinationRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetBoolean(3)),
            new { clusterId, destinationName, includeDisabled = includeDisabled ? 1 : 0 });
    }

    public async Task<int> SaveDestinationAsync(ReverseProxyDestinationRecord destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.ClusterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.DestinationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.Address);

        const string sql = @"
            IF EXISTS (SELECT 1 FROM dbo.proxy_destination WHERE cluster_id = @clusterId AND destination_name = @destinationName)
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
            END;";

        return await ExecuteAsync(sql, new
        {
            clusterId = destination.ClusterId,
            destinationName = destination.DestinationName,
            destinationAddress = destination.Address,
            isEnabled = destination.IsEnabled ? 1 : 0
        });
    }

    public async Task<int> DeleteDestinationAsync(string clusterId, string destinationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationName);

        const string sql = @"
            DELETE FROM dbo.proxy_destination
            WHERE cluster_id = @clusterId AND destination_name = @destinationName;";

        return await ExecuteAsync(sql, new { clusterId, destinationName });
    }

    private async Task<List<T>> QueryListAsync<T>(string sql, Func<DbDataReader, T> projector, object? parameters = null)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<T>();
        while (await reader.ReadAsync())
        {
            result.Add(projector(reader));
        }

        return result;
    }

    private async Task<T?> QuerySingleAsync<T>(string sql, Func<DbDataReader, T> projector, object? parameters = null)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return projector(reader);
        }

        return default;
    }

    private async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    private static void AddParameters(DbCommand command, object? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var property in parameters.GetType().GetProperties())
        {
            var value = property.GetValue(parameters);
            var parameterName = property.Name.StartsWith("@") ? property.Name : "@" + property.Name;
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameterName;
            dbParameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
    }

    private static void AddParameter(DbCommand command, string parameterName, object? value)
    {
        var dbParameter = command.CreateParameter();
        dbParameter.ParameterName = parameterName;
        dbParameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(dbParameter);
    }
}
