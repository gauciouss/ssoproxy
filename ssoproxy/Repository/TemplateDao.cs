using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ssoproxy.Repository
{
    public abstract class TemplateDao<T> where T : class
    {
        protected readonly string _connectionString;
        protected readonly ILogger<TemplateDao<T>> _logger;

        public TemplateDao(IConfiguration configuration, ILogger<TemplateDao<T>> logger)
        {
            _connectionString = configuration.GetConnectionString("MSSQL")
                ?? throw new InvalidOperationException("Missing MSSQL connection string.");
            _logger = logger;
        }

        protected abstract SqlConnection CreateConnection(string connectionString);
    }
}