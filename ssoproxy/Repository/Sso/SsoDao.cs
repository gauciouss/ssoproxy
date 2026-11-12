using NLog;
using Microsoft.Data.SqlClient;
using ssoproxy.Repository;

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

        await using var connection = new SqlConnection(_connectionFactory.GetConnectionString());
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO SSO_PROXY_TARGET (HOST_ID, HOST, REVERSE_PROXY_HOST, ORDER_NO, SYSTEM_NAME, IS_TERMINATED, CREATED_AT, UPDATED_AT)
            VALUES (@hostId, @host, @reverseProxyHost, @orderNo, @systemName, @isTerminated, @createdAt, @updatedAt)
        ";
        await using var command = new SqlCommand(sql, connection);

        AddSafeParameter(command, "@hostId", bean.HostId);
        AddSafeParameter(command, "@host", bean.Host);
        AddSafeParameter(command, "@reverseProxyHost", bean.ReverseProxyHost);
        AddSafeParameter(command, "@orderNo", bean.OrderNo);
        AddSafeParameter(command, "@systemName", bean.SystemName);
        AddSafeParameter(command, "@isTerminated", bean.IsTerminated);
        AddSafeParameter(command, "@createdAt", bean.CreatedAt);
        AddSafeParameter(command, "@updatedAt", bean.UpdatedAt);

        await command.ExecuteNonQueryAsync();

        if(bean.WhiteFilters != null && bean.WhiteFilters.Count > 0)
        {
            const string whiteFilterSql = @"
                    INSERT INTO SSO_WHITE_FILTER (HOST_ID, FILTER_PATH, CREATED_AT, UPDATED_AT)
                    VALUES (@hostId, @filterPath, @createdAt, @updatedAt)
                ";
            foreach (var whiteFilter in bean.WhiteFilters)
            {                
                await using var whiteFilterCommand = new SqlCommand(whiteFilterSql, connection);

                AddSafeParameter(whiteFilterCommand, "@hostId", whiteFilter.HostId);
                AddSafeParameter(whiteFilterCommand, "@filterPath", whiteFilter.FilterPath);
                AddSafeParameter(whiteFilterCommand, "@createdAt", whiteFilter.CreatedAt);
                AddSafeParameter(whiteFilterCommand, "@updatedAt", whiteFilter.UpdatedAt);

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

        await using var connection = new SqlConnection(_connectionFactory.GetConnectionString());
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
        await using var command = new SqlCommand(sql, connection);
        AddSafeParameter(command, "@hostId", bean.HostId);
        AddSafeParameter(command, "@host", bean.Host);
        AddSafeParameter(command, "@reverseProxyHost", bean.ReverseProxyHost);
        AddSafeParameter(command, "@orderNo", bean.OrderNo);
        AddSafeParameter(command, "@systemName", bean.SystemName);
        AddSafeParameter(command, "@isTerminated", bean.IsTerminated);
        AddSafeParameter(command, "@updatedAt", bean.UpdatedAt);

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
                await using var whiteFilterCommand = new SqlCommand(whiteFilterSql, connection);
                AddSafeParameter(whiteFilterCommand, "@hostId", whiteFilter.HostId);
                AddSafeParameter(whiteFilterCommand, "@whiteListId", whiteFilter.WhiteListId);
                AddSafeParameter(whiteFilterCommand, "@filterPath", whiteFilter.FilterPath);
                AddSafeParameter(whiteFilterCommand, "@updatedAt", whiteFilter.UpdatedAt);

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

        var cleanSystemName = SqlInjectionGuard.SanitizeText(systemName, nameof(systemName), 128);

        await using var connection = new SqlConnection(_connectionFactory.GetConnectionString());
        await connection.OpenAsync();
        
        const string sql = @"
            SELECT A.HOST_ID, A.HOST, A.REVERSE_PROXY_HOST, A.ORDER_NO, A.SYSTEM_NAME,
                   A.IS_TERMINATED, A.IS_ENABLED,
                   A.CREATED_AT AS TARGET_CREATED_AT, A.UPDATED_AT AS TARGET_UPDATED_AT,
                   B.WHITE_LIST_ID, B.FILTER_PATH,
                   B.CREATED_AT AS FILTER_CREATED_AT, B.UPDATED_AT AS FILTER_UPDATED_AT
            FROM SSO_PROXY_TARGET A
            LEFT JOIN SSO_WHITE_FILTER B ON A.HOST_ID = B.HOST_ID
            WHERE A.SYSTEM_NAME LIKE @systemName ESCAPE '\\'
        ";
        await using var command = new SqlCommand(sql, connection);

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@systemName";
        parameter.Value = $"%{SqlInjectionGuard.EscapeLikePattern(cleanSystemName)}%";
        command.Parameters.Add(parameter);
        return await GetAllSsoProxyTargets(command);    
    }

    /// <summary>
    /// 查詢所有啟用的 SSO Proxy Target 及其對應的白名單過濾器
    /// </summary>
    /// <returns></returns>
    public async Task<List<SsoProxyTarget>> GetAllEnabledSsoProxyTargets()
    {
        await using var connection = new SqlConnection(_connectionFactory.GetConnectionString());
        await connection.OpenAsync();

        const string sql = @"
            SELECT A.HOST_ID, A.HOST, A.REVERSE_PROXY_HOST, A.ORDER_NO, A.SYSTEM_NAME,
                   A.IS_TERMINATED, A.IS_ENABLED,
                   A.CREATED_AT AS TARGET_CREATED_AT, A.UPDATED_AT AS TARGET_UPDATED_AT,
                   B.WHITE_LIST_ID, B.FILTER_PATH,
                   B.CREATED_AT AS FILTER_CREATED_AT, B.UPDATED_AT AS FILTER_UPDATED_AT
            FROM SSO_PROXY_TARGET A
            LEFT JOIN SSO_WHITE_FILTER B ON A.HOST_ID = B.HOST_ID
            WHERE A.IS_ENABLED = 1
        ";
        await using var command = new SqlCommand(sql, connection);
        return await GetAllSsoProxyTargets(command);    
    }

    /// <summary>
    /// 查詢所有 SSO Proxy Target 及其對應的白名單過濾器
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private static void AddSafeParameter(SqlCommand command, string parameterName, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = SqlInjectionGuard.SanitizeParameter(value, parameterName);
        command.Parameters.Add(parameter);
    }

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
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IS_ENABLED")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("TARGET_CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("TARGET_UPDATED_AT"))
                };
                targetDictionary[hostId] = ssoProxyTarget;
            }

            var whiteListIdOrdinal = reader.GetOrdinal("WHITE_LIST_ID");
            if (!reader.IsDBNull(whiteListIdOrdinal))
            {
                var whiteFilter = new SsoWhiteFilter
                {
                    HostId = hostId,
                    WhiteListId = reader.GetInt32(whiteListIdOrdinal),
                    FilterPath = reader.GetString(reader.GetOrdinal("FILTER_PATH")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("FILTER_CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("FILTER_UPDATED_AT"))
                };
                ssoProxyTarget.WhiteFilters.Add(whiteFilter);
            }
        }

        return targetDictionary.Values.ToList();     
    }

}
