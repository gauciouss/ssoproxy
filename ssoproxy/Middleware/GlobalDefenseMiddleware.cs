using NLog;
using ssoproxy.Services;
using ssoproxy.Services.Auth;
using ssoproxy.Database.Redis;
using gd.Util;
namespace ssoproxy.Middleware
{
    /// <summary>
    /// 全局防御中间件
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
                    Logger.Info("[SSO_BYPASS] TraceId: {0}, path {1} matches the bypass rules; request allowed", traceId, requestPath);
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

                    Logger.Warn("[SSO_MISSING_TOKEN] TraceId: {0}, request has no authentication token; redirecting to the SSO login page, returnUrl={1}", traceId, returnUrl);
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
            catch (Exception ex)
            {
                Logger.Error(ex, "[SSO_GLOBAL_CRASH] TraceId: {0}, unexpected core failure", traceId);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
            }
        }
    }
}
