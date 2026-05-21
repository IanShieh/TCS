# ERP.Auth.Common — Specification

> **狀態**: 設計鎖定（經過 3 輪偵探貓審查）
> **日期**: 2026-04-17

---

## 目標

建立一個共用 JWT 認證函式庫（Razor Class Library），整合至 5 個 ERP 主檔系統，驗證由 erp_plus 核發的 JWT Token。

## 背景

- **erp_plus** (.NET 2.0 Web Forms)：手寫 HMAC-SHA256 JWT 產生器，Token 含使用者資訊 claim，金鑰來自 `Web.config JwtSecret`，效期 8 小時
- **5 個主檔系統**：AccountCodeCreate / AccountOffsetEntrySystem / AssetManagementSystem / BankAccountCreate / SupplierCreate（.NET 8/9，其中 3 個使用 DbNetSuiteCore）
- **問題**：5 個系統各自需要認證整合，需要共用函式庫避免重複

---

## 認證流程

```
erp_plus (使用者登入後)
  │
  ├─ 生成 JWT Token (HMAC-SHA256)
  │
  └─ 回傳自動提交 POST form
       │  POST /?  body: token=eyJhbG...
       ▼
  目標系統 /api/auth/token-login
       │
       ├─ 驗證 JWT (signature + expiry + claim whitelist)
       │
       ├─ 驗證通過 → 回傳主頁 HTML，內含 <script>window.__erp_token="..."</script>
       │
       └─ 驗證失敗 → 401
            │
            ▼
  erp-auth.js (DOMContentLoaded)
       │
       ├─ 讀取 window.__erp_token，存入 sessionStorage
       │
       ├─ patch window.fetch → 自動加 Authorization header
       │
       ├─ patch $.ajaxSetup + $(document).ajaxError → 自動加 header（DbNetSuiteCore 用）
       │
       └─ 後續所有 AJAX 請求自動帶 Token
```

---

## 決策記錄

| 項目 | 決策 |
|------|------|
| 發布方式 | Project Reference（不發布 NuGet） |
| SecretKey | 各專案自管 `appsettings.json Jwt:SecretKey` |
| ClockSkew | `TimeSpan.Zero`（嚴格） |
| Token 儲存 | `sessionStorage` |
| Auth Policy | `FallbackPolicy = RequireAuthenticatedUser` |
| Claim 白名單 | sub, name, role, company, department, email, jti, iat, exp, nbf |
| erp_plus JWT 安全 | 不改 erp_plus；接收端加 claim 白名單驗證 |
| .NET 版本 | `net8.0;net9.0` 多目標 |
| JWT 傳輸 | POST form（不放在 URL，避免 Server Log 記錄） |
| Hardcoded Key | 不動（repo 為 private，生產環境另設密鑰） |
| erp_plus 入口頁 | 各系統獨立入口頁，使用自動提交 POST form |

---

## API Contract

### POST /api/auth/token-login

- **Request**: `Content-Type: application/x-www-form-urlencoded`，欄位 `token`
- **成功**: HTTP 200，回傳主頁 HTML（含 `<script>window.__erp_token="..."</script>`）
- **失敗**: HTTP 401 JSON `{ "error": "..." }`

### GET /api/auth/verify

- **Request**: `Authorization: Bearer <token>` header
- **成功**: HTTP 200 JSON `{ "userId": "...", "userName": "...", "roles": [...] }`
- **失敗**: HTTP 401

---

## 元件設計

### ErpAuthExtensions（核心整合）

```csharp
// 在 Program.cs builder 階段呼叫
builder.Services.AddErpAuth(configuration);

// 在 Program.cs app 階段呼叫（需在 UseAuthorization 之後）
app.MapErpAuthEndpoints();
```

### JwtService（Token 驗證）

```csharp
public interface IJwtService
{
    JwtValidationResult ValidateToken(string token);
}
```

- 驗證 signature（HMAC-SHA256）
- 驗證 expiry（ClockSkew = 0）
- Claim 白名單檢查（拒絕未知 claim key）

### CurrentUserService（取得當前使用者）

```csharp
public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

### erp-auth.js（前端整合）

- `DOMContentLoaded` 自動初始化
- 從 `window.__erp_token` 讀取 token
- 存入 `sessionStorage`
- `window.fetch` monkey-patch（加 Authorization header，處理 401）
- `$.ajaxSetup` + `$(document).ajaxError`（DbNetSuiteCore jQuery.ajax 支援）
- 401 時清 sessionStorage 並跳轉

---

## 各專案整合清單

| 專案 | 框架 | 需修正的已知問題 |
|------|------|----------------|
| AccountCodeCreate | .NET 9, MVC + API | 無 |
| AccountOffsetEntrySystem | .NET 8, MVC + DbNetSuiteCore | 無 |
| AssetManagementSystem | .NET 9, MVC + DbNetSuiteCore | 無 |
| BankAccountCreate | .NET 8, Razor Pages + DbNetSuiteCore | UseDbNetSuiteCore 在 UseAuthorization 之前，需修正順序 |
| SupplierCreate | .NET 8, MVC (Clean Architecture) | 無 |

---

## 不在範圍內

- 不處理 erp_plus 本身的認證機制
- 不實作 Refresh Token
- 不實作角色權限管理（僅傳遞 claim，各專案自行決定）
- 不整合 No-sourceTicketingSystem（共存，不遷移）
