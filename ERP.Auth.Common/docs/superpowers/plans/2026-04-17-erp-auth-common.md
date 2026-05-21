# ERP.Auth.Common Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 建立共用 JWT 認證函式庫（RCL），整合至 5 個 ERP 主檔系統，驗證 erp_plus 核發的 JWT Token

**Architecture:** Razor Class Library 多目標（net8.0;net9.0），透過 Project Reference 整合，不發布 NuGet。提供 `AddErpAuth()` + `MapErpAuthEndpoints()` 兩個整合點，以及前端 `erp-auth.js`。

**Tech Stack:** .NET 8/9, ASP.NET Core JWT Bearer, System.IdentityModel.Tokens.Jwt, xUnit, Microsoft.AspNetCore.Mvc.Testing

---

## 檔案結構

```
ERP.Auth.Common/
├── ERP.Auth.Common.csproj              ← RCL, net8.0;net9.0
├── Models/
│   ├── JwtSettings.cs                  ← Jwt:SecretKey, Issuer, Audience 設定
│   ├── JwtValidationResult.cs          ← 驗證結果（IsValid, UserId, Claims, Error）
│   └── TokenRequest.cs                 ← POST /api/auth/token-login 的 form model
├── Services/
│   ├── IJwtService.cs                  ← 驗證 token 的介面
│   ├── JwtService.cs                   ← 實作：signature + expiry + claim whitelist
│   ├── ICurrentUserService.cs          ← 取得當前使用者的介面
│   └── CurrentUserService.cs           ← 從 IHttpContextAccessor 取 claims
├── Extensions/
│   └── ErpAuthExtensions.cs            ← AddErpAuth() (IServiceCollection) + MapErpAuthEndpoints() (WebApplication)
└── wwwroot/
    └── js/
        └── erp-auth.js                 ← auto-init, fetch patch, jQuery.ajax patch, 401 handling

ERP.Auth.Common.Tests/
├── ERP.Auth.Common.Tests.csproj        ← xUnit, net8.0
└── Services/
    └── JwtServiceTests.cs              ← JwtService 單元測試（白名單、過期、偽造）
```

---

## Task 1: 專案骨架

**Files:**
- Create: `ERP.Auth.Common/ERP.Auth.Common.csproj`
- Create: `ERP.Auth.Common.Tests/ERP.Auth.Common.Tests.csproj`

- [x] **Step 1.1**: 建立主函式庫 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.9.0" />
  </ItemGroup>
</Project>
```

- [x] **Step 1.2**: 建立測試專案 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="7.2.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ERP.Auth.Common\ERP.Auth.Common.csproj" />
  </ItemGroup>
</Project>
```

- [x] **Step 1.3**: 確認 `dotnet build` 通過（無程式碼，骨架即可）

---

## Task 2: Models

**Files:**
- Create: `ERP.Auth.Common/Models/JwtSettings.cs`
- Create: `ERP.Auth.Common/Models/JwtValidationResult.cs`
- Create: `ERP.Auth.Common/Models/TokenRequest.cs`

- [x] **Step 2.1**: 建立 `JwtSettings.cs`

```csharp
namespace ERP.Auth.Common.Models;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
```

- [x] **Step 2.2**: 建立 `JwtValidationResult.cs`

```csharp
namespace ERP.Auth.Common.Models;

public class JwtValidationResult
{
    public bool IsValid { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    public string? Error { get; init; }

    public static JwtValidationResult Success(
        string userId,
        string userName,
        IReadOnlyDictionary<string, string> claims) =>
        new() { IsValid = true, UserId = userId, UserName = userName, Claims = claims };

    public static JwtValidationResult Fail(string error) =>
        new() { IsValid = false, Error = error };
}
```

- [x] **Step 2.3**: 建立 `TokenRequest.cs`

```csharp
namespace ERP.Auth.Common.Models;

public class TokenRequest
{
    public string Token { get; set; } = string.Empty;
}
```

- [x] **Step 2.4**: `dotnet build` 確認通過

---

## Task 3: JwtService (TDD)

**Files:**
- Create: `ERP.Auth.Common/Services/IJwtService.cs`
- Create: `ERP.Auth.Common/Services/JwtService.cs`
- Test: `ERP.Auth.Common.Tests/Services/JwtServiceTests.cs`

### Step 3.1–3.4: Claim 白名單驗證（紅燈 → 綠燈）

- [x] **Step 3.1**: 先寫失敗測試

```csharp
[Fact]
public void ValidateToken_WithUnknownClaim_ReturnsFail()
{
    // 用含未知 claim 的 token，期望驗證失敗
    var token = BuildToken(new Dictionary<string, string>
    {
        ["sub"] = "user01",
        ["name"] = "測試使用者",
        ["__proto__"] = "injected"   // 未知 claim
    });

    var result = _sut.ValidateToken(token);

    result.IsValid.Should().BeFalse();
    result.Error.Should().Contain("unknown claim");
}
```

- [x] **Step 3.2**: 跑測試，確認紅燈（編譯失敗或測試失敗）
- [x] **Step 3.3**: 實作 `IJwtService` 介面

```csharp
namespace ERP.Auth.Common.Services;

public interface IJwtService
{
    JwtValidationResult ValidateToken(string token);
}
```

- [x] **Step 3.4**: 實作 `JwtService.cs`（claim 白名單邏輯）

```csharp
namespace ERP.Auth.Common.Services;

public class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    private static readonly HashSet<string> AllowedClaims = new(StringComparer.OrdinalIgnoreCase)
    {
        "sub", "name", "role", "company", "department",
        "email", "jti", "iat", "exp", "nbf"
    };

    public JwtValidationResult ValidateToken(string token)
    {
        // 1. 驗證 signature + expiry
        // 2. 白名單檢查所有 claim key
        // 3. 回傳 JwtValidationResult
    }
}
```

- [x] **Step 3.5**: 跑測試，確認綠燈

### Step 3.6–3.9: 過期 Token 驗證

- [x] **Step 3.6**: 寫失敗測試（過期 Token 應回傳 Fail）

```csharp
[Fact]
public void ValidateToken_WithExpiredToken_ReturnsFail()
{
    var token = BuildToken(validClaims, expiry: DateTime.UtcNow.AddHours(-1));
    var result = _sut.ValidateToken(token);

    result.IsValid.Should().BeFalse();
    result.Error.Should().Contain("expired");
}
```

- [x] **Step 3.7**: 跑測試，確認紅燈
- [x] **Step 3.8**: 補充 `JwtService` 過期驗證實作（ClockSkew = 0）
- [x] **Step 3.9**: 跑測試，確認綠燈

### Step 3.10–3.13: 偽造簽名驗證

- [x] **Step 3.10**: 寫失敗測試（錯誤密鑰簽名應回傳 Fail）

```csharp
[Fact]
public void ValidateToken_WithWrongSignature_ReturnsFail()
{
    var token = BuildTokenWithKey(validClaims, wrongSecretKey: "wrong-key-00000000");
    var result = _sut.ValidateToken(token);

    result.IsValid.Should().BeFalse();
}
```

- [x] **Step 3.11**: 跑測試，確認紅燈
- [x] **Step 3.12**: 確認 `JwtService` 簽名驗證實作正確
- [x] **Step 3.13**: 跑測試，確認綠燈，commit

---

## Task 4: CurrentUserService

**Files:**
- Create: `ERP.Auth.Common/Services/ICurrentUserService.cs`
- Create: `ERP.Auth.Common/Services/CurrentUserService.cs`

- [x] **Step 4.1**: 建立 `ICurrentUserService.cs`

```csharp
namespace ERP.Auth.Common.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

- [x] **Step 4.2**: 實作 `CurrentUserService.cs`（從 IHttpContextAccessor 取 claims）

```csharp
namespace ERP.Auth.Common.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string UserName => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
```

- [x] **Step 4.3**: `dotnet build` 確認通過

---

## Task 5: ErpAuthExtensions

**Files:**
- Create: `ERP.Auth.Common/Extensions/ErpAuthExtensions.cs`

- [x] **Step 5.1**: 實作 `AddErpAuth()`（IServiceCollection 擴充方法）

```csharp
public static IServiceCollection AddErpAuth(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddHttpContextAccessor();

    var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt settings are not configured.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
                ValidAudience = jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero
            };
        });

    services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    return services;
}
```

- [x] **Step 5.2**: 實作 `MapErpAuthEndpoints()`（WebApplication 擴充方法）

```csharp
public static WebApplication MapErpAuthEndpoints(this WebApplication app)
{
    // POST /api/auth/token-login
    // 接收 erp_plus POST form，驗證 token，回傳主頁（含 window.__erp_token 注入）
    app.MapPost("/api/auth/token-login", async (
        [FromForm] TokenRequest request,
        IJwtService jwtService,
        HttpResponse response) =>
    {
        var result = jwtService.ValidateToken(request.Token);
        if (!result.IsValid)
            return Results.Json(new { error = result.Error }, statusCode: 401);

        // 回傳 HTML，注入 token 供 erp-auth.js 讀取
        var html = $"""
            <!DOCTYPE html><html><head>
            <script>window.__erp_token="{request.Token}";</script>
            <meta http-equiv="refresh" content="0;url=/" />
            </head><body></body></html>
            """;
        return Results.Content(html, "text/html");
    }).AllowAnonymous();

    // GET /api/auth/verify
    app.MapGet("/api/auth/verify", (ICurrentUserService currentUser) =>
        Results.Ok(new
        {
            userId = currentUser.UserId,
            userName = currentUser.UserName,
            roles = currentUser.Roles
        }));

    return app;
}
```

- [x] **Step 5.3**: `dotnet build` 確認通過，commit

---

## Task 6: erp-auth.js

**Files:**
- Create: `ERP.Auth.Common/wwwroot/js/erp-auth.js`

- [x] **Step 6.1**: 實作核心功能

```javascript
(function () {
    'use strict';

    const STORAGE_KEY = 'erp_auth_token';

    // ── Token 管理 ──────────────────────────────────────────
    function getToken() {
        return sessionStorage.getItem(STORAGE_KEY);
    }

    function setToken(token) {
        sessionStorage.setItem(STORAGE_KEY, token);
    }

    function clearToken() {
        sessionStorage.removeItem(STORAGE_KEY);
    }

    function handleUnauthorized() {
        clearToken();
        window.location.href = '/';
    }

    // ── 初始化：從 server 注入或 URL param 讀取 token ──────
    function init() {
        // 1. 優先從 server 注入（POST flow）
        if (window.__erp_token) {
            setToken(window.__erp_token);
            delete window.__erp_token;
        }

        // 2. Fallback：URL param（向後相容）
        const urlParams = new URLSearchParams(window.location.search);
        const urlToken = urlParams.get('token');
        if (urlToken) {
            setToken(urlToken);
            history.replaceState(null, '', window.location.pathname);
        }

        // 3. patch fetch
        patchFetch();

        // 4. patch jQuery.ajax（DbNetSuiteCore 使用）
        patchJqueryAjax();
    }

    // ── fetch monkey-patch ───────────────────────────────────
    function patchFetch() {
        const originalFetch = window.fetch;
        window.fetch = function (input, init = {}) {
            const token = getToken();
            if (token) {
                init.headers = Object.assign(
                    { 'Authorization': 'Bearer ' + token },
                    init.headers || {}
                );
            }
            return originalFetch.call(this, input, init).then(function (response) {
                if (response.status === 401) {
                    handleUnauthorized();
                }
                return response;
            });
        };
    }

    // ── jQuery.ajax patch（DbNetSuiteCore）──────────────────
    function patchJqueryAjax() {
        if (typeof $ === 'undefined' || typeof $.ajaxSetup !== 'function') return;

        $.ajaxSetup({
            beforeSend: function (xhr) {
                var token = getToken();
                if (token) {
                    xhr.setRequestHeader('Authorization', 'Bearer ' + token);
                }
            }
        });

        $(document).ajaxError(function (event, xhr) {
            if (xhr.status === 401) {
                handleUnauthorized();
            }
        });
    }

    // ── 自動初始化 ───────────────────────────────────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
```

- [x] **Step 6.2**: 確認 `wwwroot/js/erp-auth.js` 路徑正確（RCL static file 路徑：`_content/ERP.Auth.Common/js/erp-auth.js`）

---

## Task 7: 各專案整合範本

每個專案的 Program.cs 整合模式：

```csharp
// ── 服務註冊 ────────────────────────────────────────────────
builder.Services.AddErpAuth(builder.Configuration);

// ── 中介軟體管線（順序重要）────────────────────────────────
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();          // ← 必須在 UseAuthorization 之前
app.UseAuthorization();
app.UseDbNetSuiteCore();          // ← BankAccountCreate 需移到這裡（原本在前面）
app.MapErpAuthEndpoints();        // ← 在所有 Map* 之前
app.MapControllers();             // 或 MapRazorPages()
```

appsettings.json 加入：

```json
{
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_HERE",
    "Issuer": "",
    "Audience": ""
  }
}
```

_Layout.cshtml 加入（在 `</body>` 前）：

```html
<script src="~/_content/ERP.Auth.Common/js/erp-auth.js"></script>
```

### 已知問題修正

**BankAccountCreate**（`Program.cs`）：
- 原本 `UseDbNetSuiteCore()` 在 `UseAuthorization()` 之前 → 需移到之後

---

## Task 8: erp_plus 入口頁範本

為每個系統在 erp_plus 建立獨立入口頁（以 AccountCodeCreate 為例）：

```csharp
// AccountCodeCreate_Entry.aspx.cs
protected void Page_Load(object sender, EventArgs e)
{
    string targetUrl = "https://account-code-system:7068";
    string token = JwtTokenHelper.Generate(Session["UserID"]?.ToString() ?? "");

    // 自動提交 POST form（Token 不出現在 URL）
    Response.Clear();
    Response.ContentType = "text/html";
    Response.Write($@"
        <!DOCTYPE html><html><body>
        <form id='f' method='POST' action='{targetUrl}/api/auth/token-login'>
          <input type='hidden' name='token' value='{token}'/>
        </form>
        <script>document.getElementById('f').submit();</script>
        </body></html>");
    Response.End();
}
```

---

## 驗收標準

- [x] `dotnet test` 所有 JwtService 單元測試通過
- [x] 偽造簽名 token 被拒絕（401）
- [x] 過期 token 被拒絕（401）
- [x] 含未知 claim 的 token 被拒絕（401）
- [x] 有效 token 通過驗證，`/api/auth/verify` 回傳使用者資訊
- [x] POST /api/auth/token-login 成功回傳含 `window.__erp_token` 的 HTML
- [x] erp-auth.js 正確注入 Authorization header（fetch + jQuery.ajax）
- [x] 401 時清 sessionStorage 並跳轉
