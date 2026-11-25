using System.Net;
using Microsoft.AspNetCore.Http;

namespace gd.Util;


/// <summary>
/// HTTP 工具類別
/// </summary>
public static class HttpUtil
{
    /// <summary>
    /// 取得客戶端IP
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetClientIp(HttpContext? context)
    {
        if (context == null)
            return string.Empty;

        // 1. 優先讀取 X-Forwarded-For (Nginx / F5 等反向代理標準標頭)
        // 格式通常為: "client_ip, proxy1_ip, proxy2_ip"，第一個即為最原始的客戶端 IP
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)            
                                      .FirstOrDefault();
            if (IsValidIp(firstIp, out var parsedForwardedIp))
            {
                return NormalizeIp(parsedForwardedIp!);
            }
        }

        // 2. 次要讀取 X-Real-IP (部分 Nginx 設定會單獨傳遞此標頭)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (IsValidIp(realIp, out var parsedRealIp))
        {
            return NormalizeIp(parsedRealIp!);
        }

        // 3. 最後退回使用連線層的 RemoteIpAddress
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            return NormalizeIp(remoteIp);
        }

        return string.Empty;
    }

    /// <summary>
    /// 檢查 IP 字串是否有效
    /// </summary>
    /// <param name="ipString"></param>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private static bool IsValidIp(string? ipString, out IPAddress? ipAddress)
    {
        ipAddress = null;
        if (string.IsNullOrWhiteSpace(ipString))
            return false;

        // 排除可能包含 Port 的情況 (例如 "203.0.113.195:54321")
        var cleanIp = ipString;
        if (ipString.Contains('.') && ipString.Contains(':'))
        {
            cleanIp = ipString.Split(':')[0];
        }

        return IPAddress.TryParse(cleanIp, out ipAddress);
    }

    /// <summary>
    /// 將 IPv4-mapped IPv6 (例如 ::ffff:192.168.1.10) 轉回純 IPv4 字串
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    private static string NormalizeIp(IPAddress ip)
    {        
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        // 本機回環統一回傳 127.0.0.1
        if (IPAddress.IsLoopback(ip))
        {
            return "127.0.0.1";
        }

        return ip.ToString();
    }
}