using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ssoproxy.Repository.Sso
{
    public class SsoProxyTarget
    {
        /// <summary>
        /// HOST_ID 為主鍵，唯一標識每個目標主機
        /// </summary>
        /// <value></value>
        public string HostId {get;set;} = string.Empty;

        /// <summary>
        /// HOST 為目標主機的域名或 IP 地址，必須唯一且不可為空
        /// </summary>
        /// <value></value>
        public string Host {get; set;} = string.Empty;

        /// <summary>
        /// REVERSE_PROXY_HOST 為反向代理的主機名稱或 IP 地址，必須不可為空
        /// </summary>
        /// <value></value>
        public string ReverseProxyHost {get; set;} = string.Empty;

        /// <summary>
        /// ORDER 為目標主機的排序，用於決定請求的處理順序
        /// </summary>
        /// <value></value>
        public int Order {get; set;}

        /// <summary>
        /// SYSTEM_NAME 為目標主機所屬的系統名稱
        /// </summary>
        /// <value></value>
        public string SystemName {get; set;} = string.Empty;

        /// <summary>
        /// IS_TERMINATED 表示該目標是否被終止，TRUE 表示受保護，FALSE 表示不受保護
        /// </summary>
        /// <value></value>
        public bool IsTerminated {get; set;} = true;

        public DateTime CreatedAt {get; set;} = DateTime.Now;

        public DateTime UpdatedAt {get; set;} = DateTime.Now;
    }
}