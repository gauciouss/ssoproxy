using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ssoproxy.Repository.Sso
{
    public interface ISsoProxyTargetDao
    {           
        /// <summary>
        /// 取得所有 SSO Proxy Target
        /// </summary>
        /// <returns></returns>
        public Task<List<SsoProxyTarget>> GetAllEnabledSsoProxyTargets();

        /// <summary>
        /// 根據系统名稱取得 SSO Proxy Target
        /// </summary>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public Task<List<SsoProxyTarget>> GetSsoProxyTargetAsync(string systemName);

        /// <summary>
        /// 儲存 SSO Proxy Target
        /// </summary>
        /// <param name="bean"></param>
        /// <returns></returns>
        public Task SaveSsoProxyTargetAsync(SsoProxyTarget bean);

        /// <summary>
        /// 更新 SSO Proxy Target
        /// </summary>
        /// <param name="bean"></param>
        /// <returns></returns>
        public Task UpdateSsoProxyTargetAsync(SsoProxyTarget bean);
    }
}