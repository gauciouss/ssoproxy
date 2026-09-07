using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ssoproxy.Database
{
    public interface IDaoTemplate
    {
        Task<List<T>> QueryAsync<T>(string sql, Func<IDataRecord, T> map, object? parameters = null);

        // 執行查詢並回傳單筆物件
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, Func<IDataRecord, T> map, object? parameters = null);

        // 執行 Insert, Update, Delete，回傳受影響列數
        Task<int> ExecuteAsync(string sql, object? parameters = null);

        // 執行回傳純量值 (如 COUNT, MAX, LAST_INSERT_ID)
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    }
}