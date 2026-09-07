using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gd.Core;

namespace ssoproxy.Core
{
    public class SsoProxyCode: gd.Core.GDExCode
    {
        public static readonly GDExCode InvalidToken = new("SSO-001", "無效的Token");
        public static readonly GDExCode UnauthorizedPath = new("SSO-002", "未註冊的路徑，請求被拒絕");

        public static readonly GDExCode TokenExpired = new("SSO-003", "Token已過期");

        public static readonly GDExCode CaptchaMismatch = new("SSO-004", "圖形驗證碼比對錯誤，請重新輸入");

        public static readonly GDExCode InvalidCredentials = new("SSO-005", "帳號或密碼錯誤");

        public static readonly GDExCode RedisConnectionError = new("SSO-006", "Redis 連線異常，請稍後再試");
        
    }
}