# ERP 子系統整合 Aspire 說明文件

> 版本：v2.2 | 更新日期：2026-04-26
> 適用對象：負責開發主檔子系統的團隊成員

---

## 一、整體架構概覽

```
瀏覽器
  │
  ▼
┌─────────────────────────────────────────────────────┐
│  ERP Gateway  (:8180)                               │
│  YARP 反向代理 + JWT 驗證                            │
└──────┬────────┬────────┬──────────┬─────────────────┘
       │        │        │          │          │
  /account-  /offset-  /asset/   /bank/   /supplier/
    code/      entry/                               ←── URL 前綴
       │        │        │          │          │
  AccountCode  Offset   AssetCRUD  Bank     Supplier
    Api       Entry                Create   Create
  (動態 port) (動態 port) ...       ...      ...
       │        │        │          │          │
       └────────┴────────┴──────────┴──────────┘
                           │
                  SQL Server 192.168.168.42
                    DB_25_0515 / DB_24_1023
```

**關鍵設計原則**：
- **Aspire AppHost** 統一啟動所有子系統，自動指派 port，不用寫死任何 port 號
- **YARP Gateway** 是唯一對外入口（:8180），所有子系統不直接暴露
- **ERP.Auth.Common** 提供共用的 JWT 認證、前端腳本、Aspire 健康檢查，子系統只需 `ProjectReference` 即可使用

---

## 二、ERP.Auth.Common 專案結構

位置：`c:\Users\1418\Documents\projects\ERP.Auth.Common\`

```
ERP.Auth.Common\
├── ERP.Auth.Common.csproj     ← 共用認證函式庫（子系統引用這個）
├── Extensions\
│   └── ErpAuthExtensions.cs   ← AddErpAuth() / MapErpAuthEndpoints()
├── Services\
│   ├── JwtService.cs          ← JWT 產生與驗證
│   └── CurrentUserService.cs  ← 取得目前登入使用者資訊
├── Models\
│   └── JwtSettings.cs         ← JWT 設定 Model
├── wwwroot\js\
│   └── erp-auth.js            ← 前端共用認證腳本（自動 patch fetch/jQuery）
│
├── ERP.AppHost\               ← Aspire 主機（負責啟動所有服務）
│   └── Program.cs             ← 在這裡新增子系統
│
├── ERP.Gateway\               ← YARP 反向代理
│   ├── Program.cs
│   └── appsettings.json       ← 在這裡新增路由規則
│
└── ERP.ServiceDefaults\       ← Aspire 共用 AddServiceDefaults()
    └── Extensions.cs
```

---

## 三、現有子系統整合對照表

下表的「JWT 設定檔」路徑皆以各子系統 workspace folder 為基準。

| 子系統 | Gateway 前綴 | AppHost 服務名稱 | PathBase 設定 | JWT 設定檔 |
|--------|------------|----------------|--------------|------------|
| AccountCodeCreate | `/account-code` | `account-code-create` | `app.UsePathBase("/account-code")` | `src/AccountCode.Api/appsettings.json` |
| AccountOffsetEntrySystem | `/offset-entry` | `account-offset-entry-system` | `app.UsePathBase("/offset-entry")` | `appsettings.Security.json` |
| AssetManagementSystem | `/asset` | `asset-management-system` | `app.UsePathBase("/asset")` | `AssetCRUD/appsettings.json` |
| BankAccountCreate | `/bank` | `bank-account-create` | `app.UsePathBase("/bank")` | `DbnetsuiteCore/appsettings.json` |
| SupplierCreate | `/supplier` | `supplier-create` | `app.UsePathBase("/supplier")` | `src/DingxinErp.Web/appsettings.json` |

**現有子系統在目前環境的 `Jwt:AllowedOrigins` 必須至少包含：**

```json
[
  "http://192.168.168.15:9922",
  "http://192.168.168.15:9911",
  "http://localhost:3513",
  "http://127.0.0.1:3513"
]
```

`Origin` 是瀏覽器送出的來源，格式是 `scheme://host:port`。`http://192.168.168.15` 和 `http://192.168.168.15:9922` 是不同來源，少填 port 會讓 `/api/auth/token-login` 回傳 `403 Forbidden`。

這份清單是目前柏林 staging / production / 本機開發環境的值。部署到其他環境時，請用 Chrome DevTools Network 裡 `token-login` request 的 `Origin` header 作準，不要直接照抄 IP 與 port。

部分子系統使用獨立安全設定檔，而不是根目錄的 `appsettings.json`。修改 JWT 設定時請以上表的「JWT 設定檔」欄位為準，尤其是 `AccountOffsetEntrySystem` 的 `appsettings.Security.json` 與 `BankAccountCreate` 的 `DbnetsuiteCore/appsettings.json`。

---

## 四、整合步驟（以新增主檔子系統為例）

假設你開發的子系統叫做 **CustomerCreate**，放在 `c:\Users\1418\Documents\projects\CustomerCreate\`。

### 步驟 1：子系統專案加入套件參考

編輯子系統的 `.csproj`，加入 ERP 共用函式庫參考：

```xml
<ItemGroup>
  <!-- ERP 共用認證函式庫 -->
  <ProjectReference Include="..\..\..\ERP.Auth.Common\ERP.Auth.Common.csproj" />
  <!-- Aspire 共用健康檢查 / OpenTelemetry / Service Discovery -->
  <ProjectReference Include="..\..\..\ERP.Auth.Common\ERP.ServiceDefaults\ERP.ServiceDefaults.csproj" />
</ItemGroup>
```

> **路徑**：`..\..\..` 表示從你的 `.csproj` 往上三層到 `projects\`，再進入 `ERP.Auth.Common\`。  
> 依你的專案深度調整相對路徑。

---

### 步驟 2：子系統 Program.cs 設定

```csharp
var builder = WebApplication.CreateBuilder(args);

// ① Aspire 必要：啟用 OpenTelemetry + 健康檢查 + Service Discovery
builder.AddServiceDefaults();

// ② 資料庫連線（改成你的 DbContext）
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ③ 你的 Repository / Service 等業務 DI ...

// ④ ERP 共用 JWT 認證（一行搞定）
builder.Services.AddErpAuth(builder.Configuration);

// ⑤ 你需要的 Controllers / Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

// ⑥ 重要：必須與 Gateway 路由前綴一致
app.UsePathBase("/customer");   // ← 改成你的前綴

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();        // 順序固定：先 Auth 再 Authz
app.UseAuthorization();

// ⑦ ERP 共用認證 endpoints（POST /api/auth/token-login、GET /api/auth/verify）
app.MapErpAuthEndpoints();

app.MapControllers();
app.MapRazorPages().AllowAnonymous();  // HTML 殼匿名；API 由 JWT 保護

// ⑧ Aspire 必要：/health、/alive
app.MapDefaultEndpoints();

app.Run();
```

> **注意事項**：
> - `AddServiceDefaults()` 必須在 `builder.Build()` **之前**的最前面呼叫
> - `UsePathBase()` 必須在 `UseStaticFiles()` **之前**，否則靜態資源路徑會錯誤
> - `UseAuthentication()` 必須在 `UseAuthorization()` 之前

---

### 步驟 3：子系統 appsettings.json 設定

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.168.42;Database=DB_25_0515;User Id=sa;Password=BLERPNEW;Encrypt=False;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "75371405berlin2025-SecureKey0123",
    "Issuer": "",
    "Audience": "",
    "AllowedOrigins": [
      "http://192.168.168.15:9922",
      "http://192.168.168.15:9911",
      "http://localhost:3513",
      "http://127.0.0.1:3513"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> **重要**：`Jwt:SecretKey` 必須與 Gateway 和 erp_plus 的設定完全一致，目前固定為 `75371405berlin2025-SecureKey0123`。
>
> **更重要**：`Jwt:AllowedOrigins` 必須填「來源 Origin」，不是目標系統 URL。
> 例如 erp_plus 從 `http://192.168.168.15:9922/index.aspx` 自動 POST 到 `http://192.168.168.29:8180/supplier/api/auth/token-login` 時，瀏覽器送出的 `Origin` 是 `http://192.168.168.15:9922`，所以白名單必須包含這個完整字串。
>
> `Jwt:AllowedOrigins` 空陣列會讓系統啟動失敗。這是刻意設計，因為 `/api/auth/token-login` 為了相容 erp_plus POST form 沒有使用 Antiforgery，Origin 白名單就是這個入口的 CSRF 防線。
>
> `localhost` 與 `127.0.0.1` 只供本機開發使用。正式部署若不需要本機來源，請移除這兩項，只保留實際 erp_plus 來源。

---

### 步驟 4：前端 _Layout.cshtml 設定

在 `</head>` 最前面加入 PathBase 注入，讓所有 JS 知道目前的路徑前綴：

```html
<head>
    <meta charset="utf-8" />
    <!-- ① PathBase 注入：所有 JS API 呼叫使用 window.__erpBasePath + '/api/...' -->
    <script>window.__erpBasePath = '@Context.Request.PathBase';</script>
    
    <!-- ② ERP 共用認證腳本（自動 patch fetch/jQuery，處理 401 跳轉） -->
    <script src="~/_content/ERP.Auth.Common/js/erp-auth.js"></script>
    
    <!-- 其他 CSS / JS ... -->
</head>
```

> **`Layout = null` 的頁面**（例如 DbNetSuiteCore 的 Index.cshtml）：  
> 必須直接在頁面的 `<head>` 內加入上述兩行，因為不走 `_Layout.cshtml`。

**前端 API 呼叫範本**（JavaScript）：

```javascript
// ✅ 正確：使用 window.__erpBasePath 前綴
const API_BASE = (window.__erpBasePath || '') + '/api/customers';

// AJAX 呼叫範例
$.ajax({
    url: API_BASE,
    method: 'GET',
    success: function(data) { ... }
});

// fetch 呼叫範例
const response = await fetch(API_BASE + '/' + customerId);
```

---

### 步驟 5：API 控制器設定

API 端點需明確標注 `[Authorize]`，因為 `FallbackPolicy` 由 `AddErpAuth()` 設為 `RequireAuthenticatedUser`，但為保持明確性建議標注：

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // FallbackPolicy=null 的情況下需要；AddErpAuth 已設 FallbackPolicy，標注更明確
public class CustomersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() { ... }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto) { ... }
}
```

---

### 步驟 6：新增到 ERP.AppHost

編輯 `c:\Users\1418\Documents\projects\ERP.Auth.Common\ERP.AppHost\ERP.AppHost.csproj`，加入 ProjectReference：

```xml
<!-- 加在現有 5 個子系統的 <ItemGroup> 內 -->
<ProjectReference Include="..\..\CustomerCreate\src\CustomerCreate.Web\CustomerCreate.Web.csproj" />
```

編輯 `c:\Users\1418\Documents\projects\ERP.Auth.Common\ERP.AppHost\Program.cs`，加入服務定義：

```csharp
// 加在既有子系統定義之後
var customer = builder.AddProject<Projects.CustomerCreate_Web>("customer-create")
    .WithExternalHttpEndpoints();

// 並在 builder.AddProject<Projects.ERP_Gateway>("gateway") 的 .WithReference() 串鏈中加入：
builder.AddProject<Projects.ERP_Gateway>("gateway")
    .WithReference(accountCode)
    .WithReference(offsetEntry)
    .WithReference(assetMgmt)
    .WithReference(bankAccount)
    .WithReference(supplier)
    .WithReference(customer)   // ← 新增這行
    .WithExternalHttpEndpoints();
```

> **服務名稱規則**：`AddProject<...>("服務名稱")` 中的字串即為 Aspire Dashboard 顯示的名稱，也是 YARP 用來 service discovery 的 key，**必須與 appsettings.json 中的 Cluster Address 一致**（見步驟 7）。

---

### 步驟 7：新增 Gateway 路由規則

編輯 `c:\Users\1418\Documents\projects\ERP.Auth.Common\ERP.Gateway\appsettings.json`。

每個子系統需要新增 **3 條路由**（依優先順序由高到低）：

| 路由名稱 | 路徑 Pattern | 授權原則 | 用途 |
|----------|------------|----------|------|
| `{prefix}-auth` | `/{prefix}/api/auth/{**catch-all}` | `Anonymous` | token-login 入口（不可加 JWT 驗證） |
| `{prefix}-api` | `/{prefix}/api/{**catch-all}` | `JwtPolicy` | 資料 API，必須帶 Bearer token |
| `{prefix}-pages` | `/{prefix}/{**catch-all}` | `Anonymous` | HTML 頁面 / 靜態資源 |

> **注意**：YARP 依靜態段數自動排定優先順序：`api/auth/**` > `api/**` > `/**`。  
> 勿在 `Routes` 物件內放入任何 `_comment` 欄位，YARP 會將其解析為 route ID 導致啟動失敗。

在 `ReverseProxy.Routes` 下加入（以 `customer` 為例）：

```json
"customer-auth":  { "ClusterId": "customer-create", "AuthorizationPolicy": "Anonymous", "Match": { "Path": "/customer/api/auth/{**catch-all}" } },
"customer-api":   { "ClusterId": "customer-create", "AuthorizationPolicy": "JwtPolicy",  "Match": { "Path": "/customer/api/{**catch-all}" } },
"customer-create":{ "ClusterId": "customer-create", "AuthorizationPolicy": "Anonymous",  "Match": { "Path": "/customer/{**catch-all}" } }
```

在 `ReverseProxy.Clusters` 下加入：

```json
"customer-create": {
  "Destinations": {
    "primary": { "Address": "http://customer-create/" }
  }
}
```

> **規則**：
> - Cluster ID（`"customer-create"`）必須與 AppHost 的服務名稱完全一致
> - `Match.Path` 前綴必須與子系統 `UsePathBase()` 完全一致
> - `Address` 的 hostname 就是 Aspire service name，用 `http://` 前綴，結尾加 `/`

---

### 步驟 8：erp_plus Web.config 入口 URL 設定與驗收

legacy `erp_plus` 的子系統入口頁會讀 `Web.config` 的環境設定，取得目標子系統根網址後，自動 POST JWT 到：

```text
{targetUrl}/api/auth/token-login
```

因此 `Web.config` 裡的 URL 只要填到 Gateway 子系統前綴，不要把 `/api/auth/token-login` 一起寫進去。

以新增 `CustomerCreate`、Gateway 前綴 `/customer` 為例：

```xml
<add key="CustomerCreateUrl_Dev"     value="http://127.0.0.2:8180/customer"/>
<add key="CustomerCreateUrl_Staging" value="http://192.168.168.29:8180/customer"/>
<add key="CustomerCreateUrl_Prod"    value="http://192.168.168.30:8180/customer"/>
```

`erp_plus` 入口頁通常依 `Environment` 決定要讀哪個 key：

| Environment | 會讀取的 key |
|-------------|-------------|
| `development` | `CustomerCreateUrl_Dev` |
| `staging` | `CustomerCreateUrl_Staging` |
| 其他值 / production | `CustomerCreateUrl_Prod` |

若該作業還沒有入口頁，請參考既有 `AccountCode/index.aspx.cs` 或 `SupplierCreate/index.aspx.cs` 的模式：

1. 確認 `Session["online_user"]` 存在，否則導回登入頁。
2. 讀取 `Session["online_action"]` 作為權限 action。
3. 用 `JwtTokenHelper.Generate(employeeId, name, action)` 產生 JWT。
4. 從 `Web.config` 讀取 `{Subsystem}Url_{Environment}`。
5. 產生隱藏 form，POST 到 `{targetUrl}/api/auth/token-login`。

**驗收流程**：

1. 啟動 Aspire / Gateway（建議用 `aspire-watchdog.ps1`）。
2. 從 `erp_plus` 登入並點選新子系統選單。
3. 在 Chrome DevTools → Network 找 `token-login` request。
4. 確認 Request URL 是 `http://192.168.168.29:8180/{prefix}/api/auth/token-login`（staging）或對應環境 URL。
5. 確認 Request Headers 的 `Origin` 是 erp_plus 來源，例如 `http://192.168.168.15:9922`，且這個值已存在於子系統 `Jwt:AllowedOrigins`。
6. 確認 response status 是 `200`，且 response headers 有 `Set-Cookie: erp_auth=...`。
7. 確認頁面導回 `/{prefix}/`，前端 API request 會帶 `Authorization: Bearer ...`。
8. 若結果是 `403`，先查 `AllowedOrigins`；若是 `404`，先查 Gateway route / `UsePathBase()`；若是連線失敗，先查 Gateway / AppHost / 防火牆。

> **部署提醒**：staging 或 production 機器上的 `erp_plus Web.config` 更新後，需要讓 ASP.NET Web Forms 站台重新載入設定；子系統 appsettings 更新後，也要重新啟動該子系統服務。

---

## 五、啟動與監控工具

### 5-1. aspire-watchdog.ps1（正式使用工具）

`aspire-watchdog.ps1` 是主要的生產啟動工具，功能包括：

- 啟動 Aspire AppHost，自動偵測所有子系統的 PID
- 等待 DCP 就緒（最多 30s），再等待子系統完成啟動（最多 90s）
- 在終端機顯示 Dashboard 登入網址（含 token）
- 持續監控 Gateway `/health`，連續失敗 3 次自動重啟整個 AppHost
- 收到 `Ctrl+C` 時清理整個程序樹（AppHost + 所有子系統）

**啟動**：

```powershell
cd C:\Users\1418\Documents\projects\ERP.Auth.Common
pwsh -File aspire-watchdog.ps1 -DashboardPassword "erp@prod2025"
```

**停止**：在終端機按 `Ctrl+C`

**可選參數**：

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `-DashboardPassword` | （空）| 覆蓋 Dashboard 登入密碼 |
| `-CheckIntervalSeconds` | `10` | Gateway 健康檢查間隔（秒）|
| `-HealthFailThreshold` | `3` | 連續失敗幾次才觸發重啟 |
| `-ServiceStartupGraceSeconds` | `90` | DCP 就緒後等待子系統的緩衝時間 |
| `-LogFile` | `%TEMP%\aspire-watchdog.log` | 日誌路徑 |

**正常啟動的 log 樣式**：

```
[OK] ═══ Aspire Watchdog 啟動 ═══
[OK] 啟動 ERP.AppHost（dotnet run）...
[INFO] AppHost PID: 12345
[OK] DCP (dcpctrl) 已就緒，開始監控子系統啟動
[OK] ✅ account-code-create  (PID 11111)
[OK] ✅ account-offset-entry  (PID 22222)
[OK] ✅ asset-management  (PID 33333)
[OK] ✅ bank-account-create  (PID 44444)
[OK] ✅ supplier-create  (PID 55555)
[OK] ✅ gateway  (PID 66666)
[DASH] ╔══════════════════════════════════╗
[DASH] ║ Aspire Dashboard 登入網址        ║
[DASH] ║ https://localhost:17207/login?t=…║
[DASH] ╚══════════════════════════════════╝
[OK] 開始監控 Gateway /health
```

---

### 5-2. 其他輔助腳本

| 腳本 | 用途 |
|------|------|
| `health-check.ps1` | 快速確認 Gateway 和各子系統的健康狀態 |
| `deploy-staging.ps1` | 部署至 Staging 環境 |
| `restart-aspire.ps1` | 重啟 AppHost（不含 watchdog 監控） |

---

### 5-3. 手動啟動（開發 / 除錯）

```powershell
cd C:\Users\1418\Documents\projects\ERP.Auth.Common\ERP.AppHost
dotnet run
```

Dashboard 登入 token 會出現在終端機輸出：
```
Login to the dashboard at https://localhost:17207/login?t=<token>
```

---

### 5-4. 存取位址

Aspire Dashboard：`https://localhost:17207`（token 每次啟動會更新）

Gateway 統一入口（**本機開發請用 `127.0.0.2`**，見 §8 dcpctrl 說明）：

| 路徑 | 子系統 |
|------|--------|
| `http://127.0.0.2:8180/account-code/` | 會計科目建立 |
| `http://127.0.0.2:8180/offset-entry/` | 科目對轉分錄 |
| `http://127.0.0.2:8180/asset/` | 資產建立作業 |
| `http://127.0.0.2:8180/bank/` | 金融機構建立 |
| `http://127.0.0.2:8180/supplier/` | 供應廠商建立 |

> **Staging / 正式環境**（192.168.168.29 等實體 IP）不受 dcpctrl 影響，直接用實際 IP 即可。

---

## 六、常見錯誤與解決方法

### Q1：靜態資源 404（CSS/JS 載入失敗）

**原因**：`UsePathBase()` 放在 `UseStaticFiles()` 之後。

**修正**：

```csharp
// ❌ 錯誤順序
app.UseStaticFiles();
app.UsePathBase("/customer");

// ✅ 正確順序
app.UsePathBase("/customer");
app.UseStaticFiles();
```

---

### Q2：API 回應 401 Unauthorized

**可能原因 1**：前端沒有帶 Bearer token。  
**確認**：開 DevTools → Network，看 Request Headers 是否有 `Authorization: Bearer ...`。  
**解決**：確認 `_Layout.cshtml` 的 `<head>` 有引入 `erp-auth.js`。

**可能原因 2**：`Jwt:SecretKey` 與 erp_plus 不一致。  
**解決**：確認 `appsettings.json` 的 `SecretKey` 為 `75371405berlin2025-SecureKey0123`。

---

### Q3：API 呼叫路徑錯誤（404，但 endpoint 存在）

**原因**：前端 JS 用了硬編碼的 `/api/...` 而不是 `(window.__erpBasePath || '') + '/api/...'`。

**修正**：

```javascript
// ❌ 錯誤：透過 Gateway 時前綴被截掉
fetch('/api/customers');

// ✅ 正確：加上 PathBase 前綴
fetch((window.__erpBasePath || '') + '/api/customers');
```

---

### Q4：Aspire Dashboard 顯示子系統 Unhealthy

**確認**：子系統的 `Program.cs` 最後有呼叫 `app.MapDefaultEndpoints()`。

```csharp
app.MapDefaultEndpoints(); // 提供 /health 和 /alive endpoints
app.Run();
```

---

### Q5：Layout=null 頁面的 `~/_content/` 路徑失效

DbNetSuiteCore 等頁面常設 `Layout = null`，Razor Tag Helper 的 `~` 路徑展開需要 `_ViewImports.cshtml` 中的 TagHelper 定義才能正確處理 PathBase。

**確認** `Pages/_ViewImports.cshtml` 或 `Views/_ViewImports.cshtml` 存在且包含：

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

---

### Q6：Gateway 啟動失敗 ─ `Route '_comment' requires Hosts or Path specified`

**原因**：在 `appsettings.json` 的 `ReverseProxy.Routes` 物件內放了 `"_comment"` 欄位。YARP 把所有 Routes 物件的 key 都當成 route ID，`"_comment"` 因為沒有 `Match.Path` 而報錯，導致整個 Gateway 崩潰。

**錯誤訊息**（`dotnet run` 或 stdout log 可見）：

```
System.AggregateException: The proxy config is invalid.
  (Route '_comment' requires Hosts or Path specified.)
```

**修正**：移除 Routes 物件內的 `"_comment"` 行。

```json
// ❌ 錯誤
"Routes": {
  "_comment": "路由說明...",
  "account-code-auth": { ... }
}

// ✅ 正確：把說明寫在 Routes 外面，或乾脆刪除
"Routes": {
  "account-code-auth": { ... }
}
```

> `"_comment"` 放在 `Jwt`、`Dashboard` 等非 YARP 管理的物件內是安全的，只有 `Routes` 不允許。

---

### Q7：本機開發 token-login 或 API 呼叫掛住（`ERR_CONNECTION_REFUSED` 或無回應）

**原因**：使用了 `localhost` 或 `127.0.0.1` 呼叫 Gateway。Aspire 的 `IsExternal=true` 讓 DCP（dcpctrl）同時佔用 `127.0.0.1:8180`，導致請求被 dcpctrl 內部 proxy 攔截而沒有回應。

**解決（本機開發）**：將所有指向 Gateway 的 URL 改用 `127.0.0.2`：

```
❌  http://localhost:8180/account-code/api/auth/token-login
❌  http://127.0.0.1:8180/account-code/api/auth/token-login
✅  http://127.0.0.2:8180/account-code/api/auth/token-login
```

`127.0.0.2` 屬於 loopback 範圍，dcpctrl 只精確綁定 `127.0.0.1`，不攔截其他 loopback 位址。Gateway.exe 的 `0.0.0.0:8180` 會接收所有位址，包含 `127.0.0.2`。

> **erp_plus Web.config** 的 `_Dev` URL 已設定為 `127.0.0.2:8180`。  
> Staging / 正式環境使用實體 IP（如 `192.168.168.29:8180`），自然不受影響。

---

### Q8：watchdog 顯示 5/6 個服務，Gateway 一直缺席

最常見的兩個原因：

| 原因 | 如何確認 | 解決 |
|------|---------|------|
| Gateway `appsettings.json` 有 `_comment` | `cd ERP.Gateway && dotnet run --no-build` 看錯誤 | 移除 `_comment`（見 Q6）|
| Gateway 建置失敗 | `cd ERP.Gateway && dotnet build` | 修正 build error 後重啟 watchdog |
| 舊 Gateway 程序佔用 port | `netstat -ano \| findstr ":8180"` | `Stop-Process -Id <PID>` 清除殘留程序 |

---

### Q9：為什麼從 erp_plus 進子系統時 `/api/auth/token-login` 回傳 403 Forbidden？

**原因**：子系統的 `Jwt:AllowedOrigins` 沒有包含 erp_plus 實際 Origin，或只填了 host 沒填 port。

錯誤範例：

```json
"AllowedOrigins": [ "http://192.168.168.15" ]
```

正確範例：

```json
"AllowedOrigins": [
  "http://192.168.168.15:9922",
  "http://192.168.168.15:9911"
]
```

**排查順序**：

1. 在 Chrome DevTools 的 Network 選到 `token-login`，看 Request Headers 的 `Origin`。
2. 把 `Origin` 完整加入 `Jwt:AllowedOrigins`，包含 `http/https` 和 port。
3. 確認改的是正確設定檔。現有子系統請看第三節表格的「JWT 設定檔」欄位。
4. 重新啟動子系統服務，讓 ASP.NET Core 重新載入 appsettings。
5. 若仍是 403，檢查反向代理或安全設備是否改寫 `Origin` header。
6. 若錯誤變成連線失敗或 404，再回頭檢查 Gateway 路由、PathBase、服務發現與防火牆。

**部署注意**：改完 appsettings 後（各子系統見第三節表格的「JWT 設定檔」欄位），要重新啟動子系統服務，ASP.NET Core 才會重新載入設定。

---

## 七、整合核對清單

整合前，用以下清單逐項確認：

**子系統本身**
- [ ] `.csproj` 加入 `ERP.Auth.Common` 和 `ERP.ServiceDefaults` 的 ProjectReference
- [ ] `Program.cs` 第一行呼叫 `builder.AddServiceDefaults()`
- [ ] `Program.cs` 呼叫 `builder.Services.AddErpAuth(...)`
- [ ] `Program.cs` 的 `app.UsePathBase("/xxx")` 在 `app.UseStaticFiles()` **之前**
- [ ] `Program.cs` 呼叫 `app.MapErpAuthEndpoints()` 和 `app.MapDefaultEndpoints()`
- [ ] `appsettings.json` 有正確的 `Jwt:SecretKey`（`75371405berlin2025-SecureKey0123`）
- [ ] `appsettings.json` 或安全設定檔有 `Jwt:AllowedOrigins`，且包含 erp_plus 的完整 origin（例如 `http://192.168.168.15:9922`，必須含 port）
- [ ] `_Layout.cshtml` 的 `<head>` 最前面有 `window.__erpBasePath` 注入
- [ ] `_Layout.cshtml` 引入了 `erp-auth.js`
- [ ] 所有 JS API 呼叫使用 `(window.__erpBasePath || '') + '/api/...'`

**ERP.AppHost 整合**
- [ ] `ERP.AppHost.csproj` 加入子系統 ProjectReference
- [ ] `ERP.AppHost/Program.cs` 用 `builder.AddProject<...>` 新增服務
- [ ] Gateway 的 `.WithReference(newService)` 已加入

**Gateway 路由**
- [ ] `ERP.Gateway/appsettings.json` 新增 3 條路由（`-auth` / `-api` / `-pages`）
- [ ] `-auth` 路由的 Path 為 `/{prefix}/api/auth/{**catch-all}`，AuthorizationPolicy 為 `"Anonymous"`
- [ ] `-api` 路由的 Path 為 `/{prefix}/api/{**catch-all}`，AuthorizationPolicy 為 `"JwtPolicy"`
- [ ] `-pages` 路由的 Path 為 `/{prefix}/{**catch-all}`，AuthorizationPolicy 為 `"Anonymous"`
- [ ] Routes 物件內**沒有** `"_comment"` 欄位
- [ ] 新增對應的 Cluster，Address 為 `http://{服務名稱}/`
- [ ] Cluster ID 與 AppHost 服務名稱完全一致

**erp_plus 入口與驗收**
- [ ] `erp_plus Web.config` 有 `{Subsystem}Url_Dev`、`{Subsystem}Url_Staging`、`{Subsystem}Url_Prod`
- [ ] `erp_plus Web.config` 的 URL 只填到 Gateway 子系統前綴，不包含 `/api/auth/token-login`
- [ ] `erp_plus Web.config` 的 `Environment` 會讀到正確環境 key
- [ ] staging / production 機器上的 `erp_plus Web.config` 已實際更新並重新載入
- [ ] 從 erp_plus 點選選單後，`token-login` request URL、Origin、status、cookie 都符合「步驟 8」驗收流程

---

## 八、關鍵概念說明

### PathBase 的作用

當 YARP Gateway 把 `/customer/api/customers` 轉發給子系統時，ASP.NET Core 收到的完整路徑還是 `/customer/api/customers`。`UsePathBase("/customer")` 告訴 ASP.NET Core：「前綴 `/customer` 是我的 base，Routing 從 `/api/customers` 開始算」，同時 `Context.Request.PathBase` 就等於 `"/customer"`，讓前端 JS 可以取得正確前綴。

### Aspire Service Discovery

`ERP.AppHost/Program.cs` 中的服務名稱（例如 `"customer-create"`）會被 Aspire 自動轉換為環境變數（`services__customer-create__http__0` 等），讓 YARP 的 `http://customer-create/` 這種 URL 能動態解析成實際 port。這就是為什麼不需要寫死 port 號。

### erp-auth.js 的作用

這個腳本在頁面載入時自動：
1. 從 `sessionStorage['erp_auth_token']` 讀取 JWT token
2. Monkey-patch `window.fetch` — 每個 AJAX 呼叫自動加上 `Authorization: Bearer <token>` header
3. Monkey-patch `jQuery.ajax` — 同上（DbNetSuiteCore 使用 jQuery）
4. 當收到 401 時自動清除 token 並跳轉到登入頁

Token 由 erp_plus（舊系統）在登入成功後 POST 到子系統的 `/api/auth/token-login` endpoint 寫入 sessionStorage，整個流程對子系統透明。

### YARP per-route JWT 授權

Gateway 的 `MapReverseProxy()` **不加** `.RequireAuthorization()`，由每條路由的 `AuthorizationPolicy` 欄位獨立控制：

- `"AuthorizationPolicy": "Anonymous"` → 加上 `[AllowAnonymous]` 覆蓋 FallbackPolicy
- `"AuthorizationPolicy": "JwtPolicy"` → 套用在 `Program.cs` 中 `AddAuthorization()` 定義的 `JwtPolicy`

這讓 token-login endpoint（匿名）和 API 端點（需 JWT）可以在同一個 Gateway 上共存。

### dcpctrl 雙重綁定問題（本機開發必知）

```
Gateway.exe 綁定：0.0.0.0:8180  （接受所有位址）
dcpctrl 綁定：    127.0.0.1:8180 （比 0.0.0.0 更精確，優先截取）
                  [::1]:8180
```

當你用 `localhost` 或 `127.0.0.1` 連 Gateway，Windows 會選擇最精確的 binding，dcpctrl 赢。dcpctrl 作為 Aspire 內部 proxy，無法把外部標記的 Gateway 請求路由回自己，導致掛住。

解決方式：改用任何不是 `127.0.0.1` 的 loopback 位址，例如 `127.0.0.2`。dcpctrl 只精確綁定 `127.0.0.1`，不佔 `127.0.0.2`；Gateway.exe 的 `0.0.0.0:8180` 則接收所有位址包含 `127.0.0.2`。
