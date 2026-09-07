using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ssoproxy.Database.Sso
{
    public interface ISsoDao
    {
        public Task<string> GetTerminatedSsoUrlAsync(string ssoName);

        public Task<List<string>> GetAllTerminatedSsoUrlsAsync();
    }
}