using NLog;
using ssoproxy.Services;
using ssoproxy.Services.Auth;
using ssoproxy.Repository.Redis;
using gd.Util;
using gd.Core;
using ssoproxy.Core;

namespace ssoproxy.Middleware
{
    /// <summary>
    /// 全局防御中间件，用于处理所有传入的 HTTP 请求，确保请求符合安全要求。
    /// 用繁體中文描述：此中间件主要负责检查请求的路径是否需要进行身份验证，如果需要，则验证请求中的 Token 是否有效。若 Token 无效或缺失，则重定向用户到 SSO 登录页面。
    /// 主要功能包括：
    /// 1. 检查请求路径是否在免验证列表中，如果是，则直接放行请求。
    /// 2. 如果请求路径需要验证，则从请求头或 Cookie 中提取 Token，并调用 TokenService 进行验证。
    /// 3. 如果 Token 验证成功，则继续处理请求；如果验证失败，则重定向用户到 SSO 登录页面。
    /// 4. 记录请求处理的日志，包括请求路径、TraceId、Token 验证结果等信息，以便于后续的调试和问题排查。
    /// 5. 捕获并处理所有未预期的异常，返回 500 错误响应，并记录异常信息。
    /// 6. 使用 Profiler 记录请求处理的时间，以便于性能分析和优化。
    /// 7. 提供详细的日志记录，包括请求开始、请求结束、Token 验证结果、异常信息等，帮助开发人员和运维人员更好地理解系统的运行状态和问题。
    /// </summary>
    public class GlobalDefenseMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private IRedisDatabase _redisDatabase;

        private ITokenService _tokenService;

        public GlobalDefenseMiddleware(RequestDelegate next, IRedisDatabase redisDatabase, ITokenService tokenService)
        {
            _next = next;
            _redisDatabase = redisDatabase;
            _tokenService = tokenService;
        }   
        

        /// <summary>
        /// 調用中间件逻辑
        /// 邏輯說明：檢查請求是否符合免驗證條件，若不符合則驗證 Token
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var profiler = new Profiler();
            string traceId = context.TraceIdentifier;
            string requestPath = context.Request.Path.Value ?? string.Empty;
            Logger.Trace(profiler.StartLog("Processing request. TraceId: {0}, path: {1}"), traceId, requestPath);   
            try
            {
                var pathValidator = context.RequestServices.GetRequiredService<IPathValidatorService>();

                if (pathValidator.IsBypassable(requestPath))
                {
                    Logger.Info(profiler.StopLog("Request processing completed successfully. TraceId: {0}, path: {1}"), traceId, requestPath);
                    await _next(context);
                    return;
                }

                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) && context.Request.Cookies.TryGetValue("sso_token", out var cookieToken))
                {
                    authHeader = $"Bearer {cookieToken}";
                }

                if (string.IsNullOrEmpty(authHeader))
                {
                    var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
                    var loginUrl = $"/Home/SsoLogin?returnUrl={Uri.EscapeDataString(returnUrl)}";

                    Logger.Info(profiler.StopLog("[SSO_MISSING_TOKEN] TraceId: {0}, request has no authentication token; redirecting to the SSO login page, returnUrl={1}"), traceId, returnUrl);
                    context.Response.Redirect(loginUrl);
                    return;
                }
                
                string? userJson = await _tokenService.ValidateTokenAsync(authHeader);

                if (!string.IsNullOrEmpty(userJson))
                {                    
                    Logger.Info(profiler.StopLog("Request processing completed successfully. TraceId: {0}, path: {1}"), traceId, requestPath);
                    await _next(context);
                }
                else
                {
                    var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
                    var loginUrl = $"/Home/SsoLogin?returnUrl={Uri.EscapeDataString(returnUrl)}";

                    Logger.Info(profiler.StopLog("[SSO_AUTH_FAIL] TraceId: {0}, token expired or not found; redirecting to the SSO login page, returnUrl={1}"), traceId, returnUrl);
                    context.Response.Redirect(loginUrl);
                }
            }                        
            catch (Exception ex) when (ex is not GDException)
            {
                Logger.Error(profiler.StopLog("[SSO_GLOBAL_ERROR] TraceId: {0}, unexpected error occurred, exception: {1}"), traceId, ex.ToString());
                /*context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");*/                                
                context.Response.Redirect("/Home/Error");
            }                        
        }
    }
}
