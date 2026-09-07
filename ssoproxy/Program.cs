using StackExchange.Redis;
using System.Text.Json;
using ssoproxy.Database;
using ssoproxy.Services;
using ssoproxy.Proxy;
using ssoproxy.Middleware;
using gd.Extensions;
using NLog;
using NLog.Web;
var Logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
try
{
    Logger.Info("******* Application starting *******");

    var builder = WebApplication.CreateBuilder(args);

    // 配置結構化 JSON Log
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });

    // 啟用 NLog 作為 host 日誌提供者
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // 註冊 HttpClient factory，供其他需要注入 HttpClient 的服務使用
    builder.Services.AddHttpClient();

    // 2. 一行程式碼動態掃描並註冊該命名空間下所有的 Service 類別
    var namespacesToScan = new[] { "ssoproxy.Services", "ssoproxy.Proxy", "ssoproxy.Database" }; // 這裡可以加入更多命名空間
    builder.Services.AddNamespaceComponents(namespacesToScan, ServiceLifetime.Singleton);


    // 3. 註冊 Redis 連線 (可依實際需求調整)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    // 將 IConnectionMultiplexer 注入到 DI，供 RedisDatabase 與其他需要的服務使用
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
    {
        return ssoproxy.Database.Redis.RedisConnector.Connect(redisConnectionString);
    });
    builder.Services.AddSingleton<ssoproxy.Database.Redis.IRedisDatabase, ssoproxy.Database.Redis.RedisDatabase>();

    // 4. 註冊 MySQL SSO DAO
    builder.Services.AddSingleton<ssoproxy.Database.SqlConnectionFactory>();
    builder.Services.AddSingleton<ssoproxy.Database.Sso.ISsoDao, ssoproxy.Database.Sso.SsoDao>();
    builder.Services.AddSingleton<ssoproxy.Services.Auth.IPathValidatorService, ssoproxy.Services.Auth.PathValidatorService>();
    builder.Services.AddSingleton<ssoproxy.Services.Auth.ILoginService, ssoproxy.Services.Auth.LoginService>();
    builder.Services.AddSingleton<ssoproxy.Services.Auth.ITokenService, ssoproxy.Services.Auth.TokenService>();

    // 5. 註冊 YARP Reverse Proxy
    builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var app = builder.Build();
    Logger.Info("Application startup");

    // 靜態檔案支援 (放行 CSS/JS 等靜態資源載入)
    app.UseStaticFiles();

    // 6. 全局防禦 Middleware (包含由 IPathValidatorService 處理的免檢驗過濾，以及 Token 驗證)
    app.UseMiddleware<GlobalDefenseMiddleware>();

    // 設定 MVC 預設路由，優先處理登入頁等本機 Controller 路由
    app.MapControllerRoute(
        name: "default",
        pattern: "Home/{action=Index}/{id?}");

    // 設定 YARP Reverse Proxy 路由 (再處理 host-based 轉發)
    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    Logger.Error(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Logger.Info("******* Application shutting down *******");
    LogManager.Shutdown();
}
