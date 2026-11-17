using NLog;
using ssoproxy.Repository;

namespace ssoproxy.Middleware;

/// <summary>
/// SecurityFilterMiddleware 是一個 ASP.NET Core 中間件，用於攔截 HTTP 請求，檢查 URL、Query、Header、Cookie 和 Form 參數中是否存在可疑的 SQL 注入模式。
/// 如果檢測到可疑輸入，將返回 400 Bad Request 並記錄警告日誌，否則將請求傳遞給下一個中間件。
/// </summary>
public class SecurityFilterMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SecurityFilterMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var suspiciousInputs = new List<string>();

        var path = request.Path.Value;
        if (!string.IsNullOrWhiteSpace(path) && SqlInjectionGuard.IsRequestValueSuspicious(path, 1024))
        {
            suspiciousInputs.Add("path");
        }

        foreach (var key in request.Query.Keys)
        {
            foreach (var value in request.Query[key])
            {

                Logger.Trace("Checking query parameter: {0}={1}", key, value);

                if (SqlInjectionGuard.IsRequestValueSuspicious(value, 1024))
                {
                    suspiciousInputs.Add($"query:{key}");
                    break;
                }
            }
        }

        foreach (var key in request.Headers.Keys)
        {
            foreach (var value in request.Headers[key])
            {
                Logger.Trace("Checking header: {0}={1}", key, value);
                if (SqlInjectionGuard.IsRequestValueSuspicious(value, 1024))
                {
                    suspiciousInputs.Add($"header:{key}");
                    break;
                }
            }
        }

        foreach (var key in request.Cookies.Keys)
        {
            Logger.Trace("Checking cookie: {0}={1}", key, request.Cookies[key]);
            if (SqlInjectionGuard.IsRequestValueSuspicious(request.Cookies[key], 1024))
            {
                suspiciousInputs.Add($"cookie:{key}");
            }
        }

        if (request.HasFormContentType)
        {
            try
            {
                var form = await request.ReadFormAsync();
                foreach (var key in form.Keys)
                {
                    foreach (var value in form[key])
                    {
                        Logger.Trace("Checking form parameter: {0}={1}", key, value);
                        if (SqlInjectionGuard.IsRequestValueSuspicious(value, 1024))
                        {
                            suspiciousInputs.Add($"form:{key}");
                            break;
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Some requests may not have a readable form payload; allow normal request flow to continue.
            }
        }

        if (suspiciousInputs.Count > 0)
        {
            var fieldList = string.Join(", ", suspiciousInputs.Distinct());
            Logger.Warn("Blocked suspicious request input. Path: {0}, Fields: {1}", request.Path.Value ?? string.Empty, fieldList);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid request input detected." });
            return;
        }

        await _next(context);
    }
}
