# 系統設計文件 (System Design Document)
## 專案名稱：Webview 邊緣防禦與單一登入代理系統 (SSO Proxy)

---

## 1. 系統概述 (Executive Summary)
本系統旨在為企業內部及後端網頁系統提供一套**非侵入式（Non-intrusive）**的邊緣防禦牆。當非網頁系統（如原生 App/Client）透過內嵌 Webview 存取受保護的網頁時，由託管於容器（Docker）環境中的 SSO Proxy 進行集中的邊緣驗證與狀態治理。

後端網頁系統（Target Web）完全解耦身分驗證邏輯，僅需信賴並讀取由 Proxy 注入的明文個資標頭（Header），藉此達到高內聚、低耦合的微服務架構轉型。

### 核心設計哲學：
* **強大邊緣，後端放手**：安全防禦完全收口在 Proxy，後端系統保持純粹。
* **無狀態化 (Stateless)**：Proxy 節點具備雙活（Active-Active）能力，隨時可橫向擴展。
* **剛性防禦 (Resilience)**：具備全域異常捕獲與 Redis 哨兵自動容災（Failover）機制。
* **數據治理 (Data Governance)**：全量結構化 JSON 檔案輸出（stdout），無縫對接 Filebeat 與 ELK 監控體系。

---

## 2. 系統架構與流程圖 (System Architecture)

### 2.1 混合架構生命週期時序圖 (UML Sequence)

```@startuml
autonumber
skinparam BoxPadding 10
skinparam ParticipantPadding 10

actor "非網頁系統 (App/Client)" as App
actor "內嵌 Webview (瀏覽器元件)" as Webview

box "SSO Proxy 邊緣防禦牆" #LightBlue
    participant "SSO Proxy" as Proxy
    database "Redis (Token 狀態庫)" as Redis
end box

participant "被保護的網頁系統 (Target Web)" as TargetWeb

== 階段一：App 初始登入取得 8hr Token ==
App -> Proxy: 1. 帳密/API Key 認證
Proxy -> Redis: 2. 產製 Token 識別碼並存入 Redis (效期 8hr, 綁定來源特徵)
Proxy --> App: 3. 回傳 8hr 有效的 Token (可以是加密的 JWE)

== 階段二：透過 Webview 開啟被保護網頁 (8hr 內無限次) ==
App -> Webview: 4. 喚起 Webview 並注入 HTTP Header\n[Authorization: Bearer <8hr_Token>]
Webview -> Proxy: 5. 發送網頁請求 (帶著 8hr_Token)

Activate Proxy
Proxy -> Proxy: 6. 基礎檢查 (Token 未變造)

	== 核心防禦：邊緣驗證與個資轉換 ==
	Proxy -> Redis: 7. 查詢此 Token 是否有效？是否被註銷？
	Activate Redis
	Redis --> Proxy: 8. 回傳：[有效] + 該用戶的登入個資 (JSON)
	Deactivate Redis

Proxy -> Proxy: 9. 抹除原始 Token Header\n將個資轉換注入新 Header ( e.g., X-User-Info: {個資...} )

Proxy -> TargetWeb: 10. 將請求轉發 (帶著明文個資 Header)
Activate TargetWeb
TargetWeb -> TargetWeb: 11. 讀取 X-User-Info (免寫登入邏輯，直接顯示個資頁)
TargetWeb --> Proxy: 12. 回傳網頁 HTML/Data
Deactivate TargetWeb

Proxy --> Webview: 13. 回傳網頁畫面給 Webview 渲染
Deactivate Proxy

== 階段三：資安介入/離職註銷 ==
... 8小時內，管理員發現異常或用戶登出 ...
Proxy -> Redis: 14. 註銷/刪除該 Token 的快取紀錄
...
Webview -> Proxy: 15. 下一次點擊網頁 (帶著已被註銷的 Token)
Activate Proxy
Proxy -> Redis: 16. 查詢
Redis --> Proxy: 17. 回傳：[已失效/不存在]
Proxy --> Webview: 18. 拒絕存取，重導向至系統錯誤或登入頁 (401/302)
Deactivate Proxy
@enduml

```
### 2.2 邊緣防禦核心邏輯 (Data Flow)
1. 攔截（Intercept）：SSO Proxy 在邊緣攔截所有 /target-web/* 的 HTTP 請求。
2. 驗證（Validate）：自 Authorization 標頭提取 Bearer Token，並向 Redis 哨兵集群查詢。若 Token 不存在或過期，中斷請求並優雅回傳 401 Unauthorized 結構化 JSON。
3. 代換（Mutate）：驗證通過後，Proxy 執行主權宣告，徹底抹除敏感的 Authorization 標頭，並將 Redis 中緩存的 JSON 個資注入全新的 X-User-Info 標頭。
4. 轉發（Forward）：透過 YARP (Yet Another Reverse Proxy) 將請求無感轉發給內部網路的 Target Web。

## 3.  技術選型與組件配置 (Technology Stack)

### 3.1 技術堆疊
* 開發框架：.NET 8.0 (永續支援版本 LTS)

* 反向代理引擎：YARP (Yet Another Reverse Proxy) 2.1.0

* 快取驅動：StackExchange.Redis 2.7.33 (內建支援 Sentinel 自動尋找 Master)

* 容器基底：Alpine Linux (極小化安全足跡，縮小攻擊面)

* 快取與容災：Redis 7.2-alpine + Redis Sentinel

### 3.2 網路連接埠拓撲 (Local PoC Environment)
為在單一開發環境模擬 4 台獨立主機的 AA 高可用架構，連接埠映射定義如下：
|組件名稱        |容器內部 Port|桌機外部 Port|容災角色定位              |
|------------|---------|---------|--------------------|
|sso-proxy-1 |8080     |8081     |前端網頁防禦第一線 (Active 1)|
|sso-proxy-2 |8080     |8082     |前端網頁防禦第一線 (Active 2)|
|redis-master|6379     |6379     |快取資料主要寫入點 (Primary) |
|redis-slave |6379     |6380     |快取資料唯讀同步點 (Replica) |
|sentinel-1  |26379    |26379    |仲裁哨兵節點 1            |
|sentinel-2  |26379    |26380    |仲裁哨兵節點 2            |

## 4. 原始碼與部署配置規格 (Source Code & Configuration Spec)
### 4.1 核心防禦代理 (Program.cs)
實作全局異常防護（防止容器無預警中斷引發 ERR_EMPTY_RESPONSE）、連線彈性重試機制、以及結構化日誌輸出。
```
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 配置控制台輸出為結構化單行 JSON 格式，對接 Filebeat -> ELK
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// 1. Redis 彈性重試連線機制 (對抗網路不確定性與哨兵初始化時差)
string? redisConnString = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redisConnString))
{
    throw new InvalidOperationException("Redis 連線字串未設定。");
}

IConnectionMultiplexer redisConnection;
int retryCount = 0;
int maxRetries = 5;

while (true)
{
    try
    {
        redisConnection = ConnectionMultiplexer.Connect(redisConnString);
        break;
    }
    catch (Exception ex)
    {
        retryCount++;
        Console.WriteLine($"[SSO_CONNECT_RETRY] Redis 連線失敗，進行第 {retryCount}/{maxRetries} 次重試... {ex.Message}");
        if (retryCount >= maxRetries) throw;
        Thread.Sleep(5000);
    }
}
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// 2. 註冊 YARP 反向代理
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
var logger = app.Logger;

// 3. 全局防禦與標頭代換 Middleware
app.Use(async (context, next) =>
{
    string traceId = context.TraceIdentifier;

    try
    {
        // 安全檢查：驗證標頭是否存在
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrEmpty(authHeader))
        {
            logger.LogWarning("[SSO_MISSING_TOKEN] TraceId: {TraceId}, 請求未夾帶驗證憑證", traceId);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized", message = "Missing Authorization token." }));
            return;
        }

        string token = authHeader.ToString().Replace("Bearer ", "").Trim();
        var db = app.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        string? userJson = await db.StringGetAsync(token);

        if (!string.IsNullOrEmpty(userJson))
        {
            logger.LogInformation("[SSO_AUTH_SUCCESS] TraceId: {TraceId}, Token 驗證成功", traceId);
            
            // 抹除敏感憑證，注入明文個資
            context.Request.Headers.Remove("Authorization");
            context.Request.Headers.Append("X-User-Info", userJson);

            await next(); // 放行交由 YARP 轉發
        }
        else
        {
            logger.LogWarning("[SSO_AUTH_FAIL] TraceId: {TraceId}, Token 已過期或不存在", traceId);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized", message = "Token has expired." }));
        }
    }
    catch (Exception ex)
    {
        // 全局異常捕獲，防止 Kestrel 斷線引發 ERR_EMPTY_RESPONSE
        logger.LogError(ex, "[SSO_GLOBAL_CRASH] TraceId: {TraceId}, 核心發生未預期潰堤", traceId);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
    }
});

app.MapReverseProxy();
app.Run();
```


### 4.2 容器編譯規格 (Dockerfile)
```
FROM [mcr.microsoft.com/dotnet/sdk:8.0-alpine](https://mcr.microsoft.com/dotnet/sdk:8.0-alpine) AS build
WORKDIR /src

COPY ["SsoProxy.csproj", "."]
RUN dotnet restore "SsoProxy.csproj"

COPY . .
RUN dotnet publish "SsoProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM [mcr.microsoft.com/dotnet/aspnet:8.0-alpine](https://mcr.microsoft.com/dotnet/aspnet:8.0-alpine) AS final
WORKDIR /app
COPY --from=build /app/publish .

# 金融資安稽核：鎖定台灣時區 (GMT+8)
RUN apk add --no-cache tzdata && \
    cp /usr/share/zoneinfo/Asia/Taipei /etc/localtime && \
    echo "Asia/Taipei" > /etc/timezone

EXPOSE 8080
ENTRYPOINT ["dotnet", "SsoProxy.dll"]
```

### 4.3 基礎設施協調編排 (docker-compose.yml)
```
version: '3.8'

networks:
  sso-network:
    driver: bridge

services:
  sso-proxy-1:
    build:
      context: ./SsoProxyProject
      dockerfile: Dockerfile
    container_name: local-sso-proxy-1
    ports:
      - "8081:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Redis=sentinel-1:26379,sentinel-2:26379,password=YourStrongPassword,serviceName=mymaster,abortConnect=false
    depends_on:
      - sentinel-1
      - sentinel-2
    networks:
      - sso-network
    restart: always

  sso-proxy-2:
    build:
      context: ./SsoProxyProject
      dockerfile: Dockerfile
    container_name: local-sso-proxy-2
    ports:
      - "8082:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Redis=sentinel-1:26379,sentinel-2:26379,password=YourStrongPassword,serviceName=mymaster,abortConnect=false
    depends_on:
      - sentinel-1
      - sentinel-2
    networks:
      - sso-network
    restart: always

  redis-master:
    image: redis:7.2-alpine
    container_name: local-redis-master
    command: redis-server --appendonly yes --requirepass YourStrongPassword --masterauth YourStrongPassword
    ports:
      - "6379:6379"
    networks:
      - sso-network
    restart: always

  redis-slave:
    image: redis:7.2-alpine
    container_name: local-redis-slave
    command: redis-server --replicaof redis-master 6379 --appendonly yes --requirepass YourStrongPassword --masterauth YourStrongPassword
    ports:
      - "6380:6379"
    depends_on:
      - redis-master
    networks:
      - sso-network
    restart: always

  sentinel-1:
    image: redis:7.2-alpine
    container_name: local-sentinel-1
    command: >
      sh -c "cp /usr/local/etc/redis/sentinel.conf /data/sentinel.conf && 
             redis-server /data/sentinel.conf --sentinel"
    ports:
      - "26379:26379"
    volumes:
      - ./redis-config:/usr/local/etc/redis
    depends_on:
      - redis-master
    links:
      - redis-master
    networks:
      - sso-network
    restart: always

  sentinel-2:
    image: redis:7.2-alpine
    container_name: local-sentinel-2
    command: >
      sh -c "cp /usr/local/etc/redis/sentinel.conf /data/sentinel.conf && 
             redis-server /data/sentinel.conf --sentinel"
    ports:
      - "26380:26379"
    volumes:
      - ./redis-config:/usr/local/etc/redis
    depends_on:
      - redis-slave
    links:
      - redis-master
    networks:
      - sso-network
    restart: always

  my-target-web:
    image: nginx:alpine
    container_name: local-target-web
    networks:
      - sso-network
```


## 5. 維運與容災測試指南 (Runbook & Verification)
### 5.1 正常訪問驗證
1. 進入 redis-master 容器寫入快取憑證：
```
docker exec -it local-redis-master redis-cli -a YourStrongPassword
SET my_8hr_test_token "{\"uid\":\"A123456789\",\"name\":\"何立偉\"}" EX 28800
```

2. 使用 Postman 發送 GET 請求至 http://localhost:8081/target-web/。
3. 夾帶標頭 Authorization: Bearer my_8hr_test_token。
4. 預期結果：成功返回後端 Nginx 歡迎網頁，且在 Proxy 容器日誌中觀測到 [SSO_AUTH_SUCCESS] 結構化日誌

### 5.2 自動容災（Failover）測試
1. 模擬主機中斷，強制停止 Master 容器：
```
docker stop local-redis-master
```
2. 觀測 docker logs local-sentinel-1。在 5 秒（down-after-milliseconds）後，哨兵集群會自動觸發投票機制。
3. 預期結果：local-redis-slave 被成功提拔為新 Master。此時再次發送網頁請求，Proxy 透過 abortConnect=false 參數與連線重試邏輯，在不崩潰、不中斷請求的前提下，自動在背景無縫飄移連線至新 Master。




## 文件版本控制
版本：v1.0.0

撰寫人：何立偉 (TPM / 系統架構師)

狀態：本地 PoC 驗證通過，準備進入小範圍灰度發布（Canary Deployment）評估。