using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ssoproxymgr.Repository;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public DbConnection CreateConnection(string? connectionName = null)
    {
        var effectiveName = string.IsNullOrWhiteSpace(connectionName) ? "MSSQL" : connectionName;
        var connectionString = _configuration.GetConnectionString(effectiveName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{effectiveName}' was not found.");
        }

        var providerName = _configuration[$"ConnectionStrings:{effectiveName}:Provider"] ?? "mssql";
        return providerName.ToLowerInvariant() switch
        {
            "mssql" or "sqlserver" or "system.data.sqlclient" => new SqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported.")
        };
    }
}
