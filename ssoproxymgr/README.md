# ssoproxymgr 管理介面

這個專案提供 Reverse Proxy 設定管理介面，可在資料庫中儲存設定、檢視 JSON 設定、手動發佈到 `ssoproxy`，並在進入管理頁面前要求登入驗證。

## 功能

- 讀取目前 Reverse Proxy 設定
- 儲存到 SQL Server
- 發佈到 `ssoproxy` 以更新 YARP runtime config
- 管理介面前置登入驗證
- 透過 `appsettings.json` 指定登入帳密

## 啟動方式

1. 先確認 `ssoproxymgr/appsettings.json` 內的資料庫連線與登入帳密已正確設定。
2. 在專案根目錄執行：

```bash
dotnet run --project ssoproxymgr/ssoproxymgr.csproj
```

3. 開啟瀏覽器並進入：

```text
http://localhost:5000/
```

## 登入驗證

進入管理頁面前，會先導向登入頁面。預設帳號密碼定義在 `ssoproxymgr/appsettings.json`：

```json
"ManagerAuth": {
  "Username": "admin",
  "Password": "admin123"
}
```

如果帳號或密碼錯誤，將停留在登入頁面並顯示錯誤訊息。

## 重要檔案

- `ssoproxymgr/Program.cs`：啟動設定、Cookie 驗證與授權
- `ssoproxymgr/Controllers/AccountController.cs`：登入與登出
- `ssoproxymgr/Controllers/ProxyController.cs`：管理頁面
- `ssoproxymgr/Controllers/ProxyManagerApiController.cs`：設定 API
- `ssoproxymgr/Views/Proxy/Index.cshtml`：管理介面
- `ssoproxymgr/appsettings.json`：資料庫連線與登入設定

## 建議

- 正式環境請變更預設帳密，不要直接使用 `admin/admin123`。
- 若要調整登入 cookie 有效時間，可在 `Program.cs` 內修改 `ExpiresUtc`。
- 發佈功能會先寫入資料庫，再呼叫 `ssoproxy` 的 Publish API，確保資料層與 runtime 層分離。