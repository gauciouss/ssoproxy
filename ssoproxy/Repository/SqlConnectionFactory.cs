using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ssoproxy.Repository;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, Func<string, DbConnection>> _providerFactories;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _providerFactories = new Dictionary<string, Func<string, DbConnection>>(StringComparer.OrdinalIgnoreCase)
        {
            ["mssql"] = connectionString => new SqlConnection(connectionString),
            ["sqlserver"] = connectionString => new SqlConnection(connectionString),
            ["system.data.sqlclient"] = connectionString => new SqlConnection(connectionString)
        };
    }

    public DbConnection CreateConnection(string? connectionName = null)
    {
        var effectiveName = string.IsNullOrWhiteSpace(connectionName) ? "MSSQL" : connectionName;
        var connectionString = GetConnectionString(effectiveName);
        var providerName = _configuration[$"ConnectionStrings:{effectiveName}:Provider"] ?? "mssql";

        if (!_providerFactories.TryGetValue(providerName, out var factory))
        {
            throw new NotSupportedException($"Database provider '{providerName}' is not supported.");
        }

        return factory(connectionString);
    }

    public string GetConnectionString(string? connectionName = null)
    {
        var effectiveName = string.IsNullOrWhiteSpace(connectionName) ? "MSSQL" : connectionName;
        var connectionString = _configuration.GetConnectionString(effectiveName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{effectiveName}' was not found.");
        }

        return connectionString;
    }

    public void RegisterProvider(string providerName, Func<string, DbConnection> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(factory);

        _providerFactories[providerName] = factory;
    }
}
