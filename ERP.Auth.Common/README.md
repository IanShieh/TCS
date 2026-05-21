# ERP.Auth.Common

柏林 ERP 系統的共用認證函式庫，提供 JWT + Cookie 混合認證、Origin 白名單 CSRF 防護、YARP Gateway，以及 .NET Aspire 多子系統編排。

---

## 專案結構

```
ERP.Auth.Common/
├── ERP.Auth.Common.csproj   ← 核心函式庫（AddErpAuth / MapErpAuthEndpoints）
├── Extensions/
│   └── ErpAuthExtensions.cs ← 擴充方法主體
├── Models/
│   ├── JwtSettings.cs       ← JWT 設定（含 AllowedOrigins 白名單）
│   ├── JwtValidationResult.cs
│   └── TokenRequest.cs
├── Services/
│   ├── JwtService.cs        ← Token 簽發 / 驗證
│   └── CurrentUserService.cs← 取得目前登入使用者資訊
│
├── ERP.AppHost/             ← .NET Aspire 編排器（啟動所有子系統 + Gateway）
├── ERP.Gateway/             ← YARP 反向代理（port 8180，唯一對外入口）
├── ERP.ServiceDefaults/     ← Aspire 共用預設（OpenTelemetry 等）
├── ERP.Launcher/            ← 獨立啟動器（非 Aspire 環境用）
│
├── ERP.Auth.Common.Tests/   ← xUnit 整合測試
│   └── Integration/
│       └── TokenLoginEndpointTests.cs
│
├── docs/
│   ├── aspire-integration-guide.md  ← 子系統整合完整指南（v2.2）
│   └── release-note-2026-04-24.md
│
├── start.cmd                ← 啟動 Aspire（自動偵測 pwsh / powershell）
├── stop-aspire.ps1          ← 停止所有程序
└── TODO.md                  ← 待辦 / 已完成紀錄
```

---

## 核心功能

### 1. `AddErpAuth()` — 認證服務註冊

在子系統的 `Program.cs` 的 `builder.Services` 階段呼叫：

```csharp
builder.Services.AddErpAuth(builder.Configuration);
```

- 支援 **JWT Bearer** 與 **Cookie** 雙模式，自動依請求的 `Authorization` header 切換
- Cookie 名稱：`erp_auth`，有效期 8 小時，`HttpOnly + SameSite=Lax`
- API 路徑（`/api`、`/dbnetlink`）未授權時回傳 `401`，其餘路徑導向登入頁

### 2. `MapErpAuthEndpoints()` — 認證 API 端點

在子系統的 `app` 階段呼叫：

```csharp
app.MapErpAuthEndpoints();
```

| 端點 | 說明 |
|------|------|
| `POST /api/auth/token-login` | 接收 erp_plus 傳入的 JWT，驗證後寫入 Cookie |
| `POST /api/auth/logout` | 清除 Cookie，登出 |
| `GET  /api/auth/me` | 回傳目前登入的使用者 Claims |

> **Origin 白名單**：`/api/auth/token-login` 只接受 `Jwt:AllowedOrigins` 清單內的來源，其他一律 `403`。

### 3. `ERP.Gateway` — YARP 反向代理

- 固定對外 port：**`8180`**
- 子系統前綴路由：

| 前綴 | 子系統 |
|------|--------|
| `/account-code` | AccountCodeCreate |
| `/offset-entry` | AccountOffsetEntrySystem |
| `/asset` | AssetManagementSystem |
| `/bank` | BankAccountCreate |
| `/supplier` | SupplierCreate |

- Gateway 集中驗證 JWT，子系統信任 Gateway 轉發的 Header
- 內建 `/dashboard` 狀態監控頁（Cookie 驗證，8 小時 session）

---

## 快速開始

### 啟動（開發環境）

```cmd
start.cmd
```

Aspire Dashboard 網址通常為 `http://localhost:18888`，Gateway 入口為 `http://localhost:8180`。

### 停止

```powershell
.\stop-aspire.ps1
```

### 執行測試

```bash
dotnet test ERP.Auth.Common.Tests/ERP.Auth.Common.Tests.csproj
```

---

## 子系統整合設定

### appsettings.json 必要欄位

```json
{
  "Jwt": {
    "SecretKey": "<與 Auth.Common 相同的金鑰>",
    "Issuer": "",
    "Audience": "",
    "AllowedOrigins": [
      "http://192.168.168.15:9922",
      "http://192.168.168.15:9911",
      "http://localhost:3513",
      "http://127.0.0.1:3513"
    ]
  }
}
```

> **重要**：`AllowedOrigins` 必須填完整的 `scheme://host:port`，遺漏 port 會導致 `403`。

### Program.cs 最小設定

```csharp
builder.Services.AddErpAuth(builder.Configuration);
// ...
app.UseAuthentication();
app.UseAuthorization();
app.MapErpAuthEndpoints();
```

### erp_plus 入口 Web.config

```xml
<add key="SupplierCreateUrl_Staging" value="http://192.168.168.29:8180/supplier" />
<add key="SupplierCreateUrl_Prod"    value="http://192.168.168.30:8180/supplier" />
<add key="SupplierCreateUrl_Dev"     value="http://127.0.0.2:8180/supplier" />
```

URL 只填到 Gateway 子系統前綴，**不含** `/api/auth/token-login`（erp_plus 入口頁自動補上）。

---

## 詳細文件

完整整合步驟、設定範例、驗收流程請參閱：

📄 [docs/aspire-integration-guide.md](docs/aspire-integration-guide.md)（v2.2）

---

## 技術規格

| 項目 | 值 |
|------|----|
| 目標框架 | `net8.0` / `net9.0` |
| 認證方式 | JWT HS256 + Cookie（hybrid） |
| CSRF 防護 | Origin whitelist（`Jwt:AllowedOrigins`） |
| Gateway | YARP（Microsoft.ReverseProxy） |
| 編排 | .NET Aspire |
| 測試框架 | xUnit |
