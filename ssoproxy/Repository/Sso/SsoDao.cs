using System.Data.Common;
using NLog;

namespace ssoproxy.Repository.Sso;

public sealed class SsoDao : ISsoDao
{
    private readonly SqlConnectionFactory _connectionFactory;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SsoDao(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));        
    }

    public async Task<List<SsoProxyTarget>> GetSsoProxyTargetAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            return new List<SsoProxyTarget>();
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        
        const string sql = @"
            SELECT * FROM SSO_PROXY_TARGET                 
                LEFT JOIN SSO_WHITE_FILTER ON SSO_PROXY_TARGET.HOST_ID = SSO_WHITE_FILTER.HOST_ID
            WHERE SYSTEM_NAME LIKE @systemName
        ";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@systemName";
        parameter.Value = systemName;
        command.Parameters.Add(parameter);

        var targetDictionary = new Dictionary<string, SsoProxyTarget>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var hostId = reader.GetString(reader.GetOrdinal("HOST_ID"));
            if (!targetDictionary.TryGetValue(hostId, out var ssoProxyTarget))
            {
                ssoProxyTarget = new SsoProxyTarget
                {
                    HostId = hostId,
                    Host = reader.GetString(reader.GetOrdinal("HOST")),
                    ReverseProxyHost = reader.GetString(reader.GetOrdinal("REVERSE_PROXY_HOST")),
                    Order = reader.GetInt32(reader.GetOrdinal("ORDER")),
                    SystemName = reader.GetString(reader.GetOrdinal("SYSTEM_NAME")),
                    IsTerminated = reader.GetBoolean(reader.GetOrdinal("IS_TERMINATED")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
                };
                targetDictionary[hostId] = ssoProxyTarget;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("FILTER_PATH")))
            {
                var whiteFilter = new SsoWhiteFilter
                {
                    HostId = hostId,
                    WhiteListId = reader.GetInt32(reader.GetOrdinal("WHITE_LIST_ID")),
                    FilterPath = reader.GetString(reader.GetOrdinal("FILTER_PATH")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
                };
                ssoProxyTarget.WhiteFilters.Add(whiteFilter);
            }
        }

        return targetDictionary.Values.ToList();        
    }

    
    public async Task<List<SsoProxyTarget>> GetAllSsoProxyTargets()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT * FROM SSO_PROXY_TARGET                 
                LEFT JOIN SSO_WHITE_FILTER ON SSO_PROXY_TARGET.HOST_ID = SSO_WHITE_FILTER.HOST_ID            
        ";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();

        var ssoProxyTargets = new List<SsoProxyTarget>();
        while (await reader.ReadAsync())
        {
            var ssoProxyTarget = new SsoProxyTarget
            {
                HostId = reader.GetString(reader.GetOrdinal("HOST_ID")),
                Host = reader.GetString(reader.GetOrdinal("HOST")),
                ReverseProxyHost = reader.GetString(reader.GetOrdinal("REVERSE_PROXY_HOST")),
                Order = reader.GetInt32(reader.GetOrdinal("ORDER")),
                SystemName = reader.GetString(reader.GetOrdinal("SYSTEM_NAME")),
                IsTerminated = reader.GetBoolean(reader.GetOrdinal("IS_TERMINATED")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
            };
            ssoProxyTargets.Add(ssoProxyTarget);
        }
        return ssoProxyTargets;
    }

}
