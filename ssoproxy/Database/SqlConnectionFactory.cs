using System.Data.Common;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace ssoproxy.Database;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, Func<string, DbConnection>> _providerFactories;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _providerFactories = new Dictionary<string, Func<string, DbConnection>>(StringComparer.OrdinalIgnoreCase)
        {
            ["mysql"] = connectionString => new MySqlConnection(connectionString),
            ["mysqll"] = connectionString => new MySqlConnection(connectionString)
        };
    }

    public DbConnection CreateConnection(string? connectionName = null)
    {
        var effectiveName = string.IsNullOrWhiteSpace(connectionName) ? "MySql" : connectionName;
        var connectionString = GetConnectionString(effectiveName);
        var providerName = _configuration[$"ConnectionStrings:{effectiveName}:Provider"] ?? "mysql";

        if (!_providerFactories.TryGetValue(providerName, out var factory))
        {
            throw new NotSupportedException($"Database provider '{providerName}' is not supported.");
        }

        return factory(connectionString);
    }

    public string GetConnectionString(string? connectionName = null)
    {
        var effectiveName = string.IsNullOrWhiteSpace(connectionName) ? "MySql" : connectionName;
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
