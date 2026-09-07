using System.Data.Common;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ssoproxy.Database.Sso;

public sealed class SsoDao : ISsoDao
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<SsoDao> _logger;

    public SsoDao(SqlConnectionFactory connectionFactory, ILogger<SsoDao> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    public async Task<string> GetTerminatedSsoUrlAsync(string ssoName)
    {
        if (string.IsNullOrWhiteSpace(ssoName))
        {
            return string.Empty;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT terminated_sso_url FROM sso_config WHERE sso_name = @ssoName LIMIT 1";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@ssoName";
        parameter.Value = ssoName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    public async Task<List<string>> GetAllTerminatedSsoUrlsAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT terminated_sso_url FROM sso_config WHERE terminated_sso_url IS NOT NULL AND terminated_sso_url <> '' ORDER BY sso_name";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();

        var urls = new List<string>();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                urls.Add(reader.GetString(0));
            }
        }

        return urls;
    }

    
}
