using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace ssoproxy.Database
{
    public abstract class DaoTemplate : IDaoTemplate
    {

        // 核心：接受任何繼承自 DbConnection 的連線 (SqlConnection, MySqlConnection, NpgsqlConnection)
        private readonly DbConnection _connection;

        public DaoTemplate(DbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 確保連線為開啟狀態
        /// </summary>
        private async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        public async Task<List<T>> QueryAsync<T>(string sql, Func<IDataRecord, T> map, object? parameters = null)
        {
            await EnsureConnectionOpenAsync();
            using var command = CreateCommand(sql, parameters);
            using var reader = await command.ExecuteReaderAsync();

            var list = new List<T>();
            while (await reader.ReadAsync())
            {
                list.Add(map(reader));
            }
            return list;
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, Func<IDataRecord, T> map, object? parameters = null)
        {
            await EnsureConnectionOpenAsync();
            using var command = CreateCommand(sql, parameters);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return map(reader);
            }
            return default;
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            await EnsureConnectionOpenAsync();
            using var command = CreateCommand(sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            await EnsureConnectionOpenAsync();
            using var command = CreateCommand(sql, parameters);
            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return default;

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// 通用 Command 與參數建構邏輯 (SQL Injection 防護)
        /// </summary>
        private DbCommand CreateCommand(string sql, object? parameters)
        {
            var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (parameters != null)
            {
                // 利用反射將匿名物件 (如 new { Account = "admin" }) 轉為 DbParameter
                foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                {
                    var parameter = command.CreateParameter();
                    // 處理不同 DB 的參數字首 (@ account -> @account 或 :account)
                    string paramName = prop.Name.StartsWith("@") || prop.Name.StartsWith(":") ? prop.Name : "@" + prop.Name;

                    parameter.ParameterName = paramName;
                    parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }

    }
}