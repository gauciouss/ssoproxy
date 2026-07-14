using Microsoft.AspNetCore.Http;
using NLog;
using ssoproxy.Services;
using System;
using System.Threading.Tasks;

namespace ssoproxy.Middleware
{
    public class GlobalDefenseMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public GlobalDefenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string traceId = context.TraceIdentifier;
            string requestPath = context.Request.Path.Value ?? string.Empty;

            try
            {
                var pathValidator = context.RequestServices.GetRequiredService<IPathValidatorService>();

                if (pathValidator.IsBypassable(requestPath))
                {
                    Logger.Info("[SSO_BYPASS] TraceId: {0}, 路徑 {1} 符合免驗證條件，直接放行", traceId, requestPath);
                    await _next(context);
                    return;
                }

                if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrEmpty(authHeader))
                {
                    Logger.Warn("[SSO_MISSING_TOKEN] TraceId: {0}, 請求未夾帶驗證憑證，轉導至 SSO 登入頁面", traceId);
                    context.Response.Redirect("/Home/SsoLogin");
                    return;
                }

                var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
                string? userJson = await tokenService.ValidateTokenAsync(authHeader);

                if (!string.IsNullOrEmpty(userJson))
                {
                    Logger.Info("[SSO_AUTH_SUCCESS] TraceId: {0}, Token 驗證成功", traceId);
                    context.Request.Headers.Remove("Authorization");
                    context.Request.Headers.Append("X-User-Info", userJson);
                    await _next(context);
                }
                else
                {
                    Logger.Warn("[SSO_AUTH_FAIL] TraceId: {0}, Token 已過期或不存在，轉導至 SSO 登入頁面", traceId);
                    context.Response.Redirect("/Home/SsoLogin");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SSO_GLOBAL_CRASH] TraceId: {0}, 核心發生未預期潰堤", traceId);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
            }
        }
    }
}
