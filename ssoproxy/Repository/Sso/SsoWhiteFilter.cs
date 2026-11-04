using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ssoproxy.Repository.Sso
{
    public class SsoWhiteFilter
    {
        /// <summary>
        /// 獲取或設置主機 ID
        /// </summary>
        /// <value></value>
        public string HostId { get; set; } = string.Empty;

        /// <summary>
        /// 獲取或設置白名單 ID
        /// </summary>
        /// <value></value>
        public int WhiteListId { get; set; }
        /// <summary>
        /// 獲取或設置過濾路徑
        /// </summary>
        /// <value></value>
        public string FilterPath { get; set; } = string.Empty;
        /// <summary>
        /// 獲取或設置創建時間
        /// </summary>
        /// <value></value>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>
        /// 獲取或設置更新時間
        /// </summary>
        /// <value></value>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}