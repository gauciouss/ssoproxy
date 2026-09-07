using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;


namespace ssoproxy.Database
{
    public abstract class TemplateDao<T> where T : class
    {
        protected readonly string _connectionString;
        protected readonly ILogger<TemplateDao<T>> _logger;

        public TemplateDao(IConfiguration configuration, ILogger<TemplateDao<T>> logger)
        {
            _connectionString = configuration.GetConnectionString("MySql")
                ?? throw new InvalidOperationException("Missing MySql connection string.");
            _logger = logger;
        }

        
    


        
        protected abstract MySqlConnection CreateConnection(string connectionString);
        
    }
}