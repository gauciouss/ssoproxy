using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace ssoproxy.Services;

public class PathValidatorService : IPathValidatorService
{
    private readonly List<Regex> _whitelistRegexList;
    
    // 登入頁面與提交請求的綠色通道路徑
    private static readonly string[] SsoLoginPaths = new[]
    {
        "/Home/SsoLogin",
        "/Home/SubmitSsoLogin"
    };

    public PathValidatorService(IConfiguration configuration)
    {
        // 從組態載入白名單 Patterns 並預先編譯 (提升執行期效能)
        var whitelistPatterns = configuration.GetSection("SsoWhitelist").Get<string[]>() ?? Array.Empty<string>();
        _whitelistRegexList = whitelistPatterns
            .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();
    }

    public bool IsBypassable(string requestPath)
    {
        if (string.IsNullOrEmpty(requestPath))
        {
            return false;
        }

        // 1. 優先檢查是否屬於登入相關綠色通道 (避免無窮重導向)
        if (SsoLoginPaths.Any(p => requestPath.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 2. 檢查是否命中 appsettings 中的白名單 Regex 規則
        return _whitelistRegexList.Any(regex => regex.IsMatch(requestPath));
    }
}
