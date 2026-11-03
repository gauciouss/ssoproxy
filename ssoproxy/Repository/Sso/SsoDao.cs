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

    public async Task<SsoProxyTarget> GetSsoProxyTargetAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            return new SsoProxyTarget();
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        
        const string sql = "SELECT * FROM SSO_PROXY_TARGET WHERE SYSTEM_NAME LIKE @systemName";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@systemName";
        parameter.Value = systemName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        if(result != null && result != DBNull.Value)
        {
            var ssoProxyTarget = new SsoProxyTarget
            {
                HostId = (string)result,
                Host = (string)result,
                ReverseProxyHost = (string)result,
                Order = (int)result,
                SystemName = (string)result,
                IsTerminated = (bool)result,
                CreatedAt = (DateTime)result,
                UpdatedAt = (DateTime)result
            };
            return ssoProxyTarget;
        }
        else
        {
            return new SsoProxyTarget();
        }
    }

    
    public async Task<List<SsoProxyTarget>> GetAllSsoProxyTargets()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM SSO_PROXY_TARGET";
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
