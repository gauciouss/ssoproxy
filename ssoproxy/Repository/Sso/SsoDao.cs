using System.Data.Common;
using NLog;

namespace ssoproxy.Repository.Sso;

public sealed class SsoProxyTargetDao : ISsoProxyTargetDao
{
    private readonly SqlConnectionFactory _connectionFactory;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SsoProxyTargetDao(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));        
    }

    /// <summary>
    /// 保存 SSO Proxy Target
    /// </summary>
    /// <param name="bean"></param>
    /// <exception cref="ArgumentNullException"></exception> <summary>
    /// 
    /// </summary>
    /// <param name="bean"></param>
    /// <returns></returns>
    public async Task SaveSsoProxyTargetAsync(SsoProxyTarget bean)
    {
        if (bean == null)
        {
            throw new ArgumentNullException(nameof(bean));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO SSO_PROXY_TARGET (HOST_ID, HOST, REVERSE_PROXY_HOST, ORDER, SYSTEM_NAME, IS_TERMINATED, CREATED_AT, UPDATED_AT)
            VALUES (@hostId, @host, @reverseProxyHost, @order, @systemName, @isTerminated, @createdAt, @updatedAt)
        ";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.AddWithValue("@hostId", bean.HostId);
        command.Parameters.AddWithValue("@host", bean.Host);
        command.Parameters.AddWithValue("@reverseProxyHost", bean.ReverseProxyHost);
        command.Parameters.AddWithValue("@orderNo", bean.OrderNo);
        command.Parameters.AddWithValue("@systemName", bean.SystemName);
        command.Parameters.AddWithValue("@isTerminated", bean.IsTerminated);
        command.Parameters.AddWithValue("@createdAt", bean.CreatedAt);
        command.Parameters.AddWithValue("@updatedAt", bean.UpdatedAt);

        await command.ExecuteNonQueryAsync();

        if(bean.WhiteFilters != null && bean.WhiteFilters.Count > 0)
        {
            const string whiteFilterSql = @"
                    INSERT INTO SSO_WHITE_FILTER (HOST_ID, FILTER_PATH, CREATED_AT, UPDATED_AT)
                    VALUES (@hostId, @filterPath, @createdAt, @updatedAt)
                ";
            foreach (var whiteFilter in bean.WhiteFilters)
            {                
                await using var whiteFilterCommand = connection.CreateCommand();
                whiteFilterCommand.CommandText = whiteFilterSql;

                whiteFilterCommand.Parameters.AddWithValue("@hostId", whiteFilter.HostId);
                whiteFilterCommand.Parameters.AddWithValue("@filterPath", whiteFilter.FilterPath);
                whiteFilterCommand.Parameters.AddWithValue("@createdAt", whiteFilter.CreatedAt);
                whiteFilterCommand.Parameters.AddWithValue("@updatedAt", whiteFilter.UpdatedAt);

                await whiteFilterCommand.ExecuteNonQueryAsync();
            }
        }
    }


    /// <summary>
    /// 更新 SSO Proxy Target
    /// </summary>
    /// <param name="bean"></param>
    /// <returns></returns>
    public async Task UpdateSsoProxyTargetAsync(SsoProxyTarget bean)
    {
        if (bean == null)
        {
            throw new ArgumentNullException(nameof(bean));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            UPDATE SSO_PROXY_TARGET
            SET HOST = @host,
                REVERSE_PROXY_HOST = @reverseProxyHost,
                ORDER_NO = @orderNo,
                SYSTEM_NAME = @systemName,
                IS_TERMINATED = @isTerminated,
                UPDATED_AT = @updatedAt
            WHERE HOST_ID = @hostId
        ";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@hostId", bean.HostId);
        command.Parameters.AddWithValue("@host", bean.Host);
        command.Parameters.AddWithValue("@reverseProxyHost", bean.ReverseProxyHost);
        command.Parameters.AddWithValue("@orderNo", bean.OrderNo);
        command.Parameters.AddWithValue("@systemName", bean.SystemName);
        command.Parameters.AddWithValue("@isTerminated", bean.IsTerminated);
        command.Parameters.AddWithValue("@updatedAt", bean.UpdatedAt);

        await command.ExecuteNonQueryAsync();

        if(bean.WhiteFilters != null && bean.WhiteFilters.Count > 0)
        {
            const string whiteFilterSql = @"
                    UPDATE SSO_WHITE_FILTER
                    SET FILTER_PATH = @filterPath,
                        UPDATED_AT = @updatedAt
                    WHERE HOST_ID = @hostId AND WHITE_LIST_ID = @whiteListId
                ";

            foreach (var whiteFilter in bean.WhiteFilters)
            {                
                await using var whiteFilterCommand = connection.CreateCommand();
                whiteFilterCommand.CommandText = whiteFilterSql;
                whiteFilterCommand.Parameters.AddWithValue("@hostId", whiteFilter.HostId);
                whiteFilterCommand.Parameters.AddWithValue("@whiteListId", whiteFilter.WhiteListId);
                whiteFilterCommand.Parameters.AddWithValue("@filterPath", whiteFilter.FilterPath);
                whiteFilterCommand.Parameters.AddWithValue("@updatedAt", whiteFilter.UpdatedAt);

                await whiteFilterCommand.ExecuteNonQueryAsync();
            }
        }
    }


    /// <summary>
    /// 查詢指定系統名稱的 SSO Proxy Target 及其對應的白名單過濾器
    /// </summary>
    /// <param name="systemName"></param>
    /// <returns></returns>
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
        return await GetAllSsoProxyTargets(command);    
    }

    /// <summary>
    /// 查詢所有啟用的 SSO Proxy Target 及其對應的白名單過濾器
    /// </summary>
    /// <returns></returns>
    public async Task<List<SsoProxyTarget>> GetAllEnabledSsoProxyTargets()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT * FROM SSO_PROXY_TARGET A
                LEFT JOIN SSO_WHITE_FILTER B ON A.HOST_ID = B.HOST_ID      
            WHERE A.IS_ENABLED = TRUE
        ";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await GetAllSsoProxyTargets(command);    
    }

    /// <summary>
    /// 查詢所有 SSO Proxy Target 及其對應的白名單過濾器
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private async Task<List<SsoProxyTarget>> GetAllSsoProxyTargets(SqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var targetDictionary = new Dictionary<string, SsoProxyTarget>();
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
                    OrderNo = reader.GetInt32(reader.GetOrdinal("ORDER_NO")),
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

}
