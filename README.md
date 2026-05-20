# TCS — 受訓證件作業系統

柏林 ERP 子系統之一，負責管理員工證照主檔、廠別需求及受訓紀錄，並提供 Excel 匯出功能。

---

## 專案結構

```
TCS/
├── src/
│   ├── TCS.Web            ← ASP.NET Core 8 Web（MVC + API Controllers）
│   ├── TCS.Core           ← 業務邏輯、DTOs、服務介面、驗證器
│   └── TCS.Infrastructure ← EF Core 資料存取、Migration、Repository
├── tests/
│   └── TCS.Tests          ← 單元測試（xUnit）
├── ERP.Auth.Common/       ← 共用認證函式庫（JWT + Cookie hybrid）
└── docs/                  ← TODO 清單、規格文件
```

---

## 技術規格

| 項目 | 說明 |
|------|------|
| 目標框架 | .NET 8 |
| Web 框架 | ASP.NET Core 8 MVC / Web API |
| ORM | Entity Framework Core 8 |
| 資料庫 | SQL Server 2016+（開發可用 InMemory） |
| 認證 | JWT HS256 + Cookie（由 ERP.Auth.Common 提供） |
| 驗證 | FluentValidation 11 |
| Excel 匯出 | ClosedXML |
| API 文件 | Swagger（開發環境） |

---

## 快速啟動（開發環境）

開發模式下預設使用 **InMemory 資料庫**，無需安裝 SQL Server，啟動後自動 Seed 範例資料。

```bash
cd src/TCS.Web
dotnet run
```

啟動後存取：
- 主頁面：`http://localhost:3513/tcs/`
- Swagger：`http://localhost:3513/tcs/swagger`

> **注意**：直接存取會因未登入而導向 `/tcs/session-expired`。正式使用需透過 erp_plus SSO 登入，或使用有效的 JWT 呼叫 `POST /tcs/api/auth/token-login`。

---

## 正式環境部署

### 1. 設定 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<HOST>;Database=TCS;User Id=<USER>;Password=<PASS>;"
  },
  "USE_INMEMORY_DB": "false",
  "Jwt": {
    "SecretKey": "<與 erp_plus 相同的金鑰，至少 32 bytes>",
    "Issuer": "",
    "Audience": "",
    "AllowedOrigins": [
      "http://192.168.168.15:9922",
      "http://192.168.168.15:9911"
    ]
  }
}
```

> `AllowedOrigins` 必須填 erp_plus 主機的完整 `scheme://host:port`，否則 SSO 登入會回 `403`。

### 2. 執行資料庫 Migration

```bash
dotnet ef database update \
  --project src/TCS.Infrastructure \
  --startup-project src/TCS.Web
```

### 3. 啟動

```bash
dotnet run --project src/TCS.Web --environment Production
```

---

## 認證流程（erp_plus SSO）

```
erp_plus
  │
  └─ POST /tcs/api/auth/token-login  (form: token=<JWT>)
        │
        ├─ Origin 白名單檢查（AllowedOrigins）
        ├─ JWT 驗證（HS256，exp、iss 視設定而定）
        └─ 成功 → 寫入 erp_auth Cookie（HttpOnly, 8h）→ 導向 /tcs/
```

登入後所有頁面與 API 均以 Cookie 驗證。若以程式呼叫 API，可改用 `Authorization: Bearer <JWT>` Header。

---

## 操作權限（action claim）

JWT payload 的 `action` 欄位（逗號分隔）控制各操作的存取：

| action 值 | 對應操作 |
|-----------|----------|
| `新增` | 建立證照、受訓紀錄 |
| `修改` | 更新證照、受訓紀錄 |
| `刪除` | 刪除證照、受訓紀錄 |
| `列印` | 匯出 Excel |

範例 JWT payload：

```json
{
  "employeeId": "A001",
  "name": "張三",
  "action": "新增,修改,刪除,列印"
}
```

未具備對應 action 時，API 回傳 `403`：

```json
{ "message": "您沒有此操作權限：列印" }
```

---

## 主要 API 端點

所有 API 路徑均以 `/tcs` 為 PathBase（正式環境透過 Gateway 路由時前綴為 `/tcs`）。

| 方法 | 路徑 | 說明 | 所需 action |
|------|------|------|-------------|
| GET | `/api/licenses` | 查詢證照主檔（分頁＋篩選） | — |
| POST | `/api/licenses` | 新增證照 | 新增 |
| PUT | `/api/licenses/{licenseType}` | 修改證照 | 修改 |
| DELETE | `/api/licenses/{licenseType}` | 刪除證照 | 刪除 |
| GET | `/api/training-headers` | 查詢受訓單頭 | — |
| POST | `/api/training-headers` | 新增受訓單頭 | 新增 |
| PUT | `/api/training-headers/{employeeId}/{licenseType}` | 修改受訓單頭 | 修改 |
| DELETE | `/api/training-headers/{employeeId}/{licenseType}` | 刪除受訓單頭 | 刪除 |
| GET | `/api/export/training-headers` | 匯出受訓單頭 Excel | 列印 |
| POST | `/api/auth/token-login` | erp_plus SSO 登入 | — |
| GET | `/api/auth/verify` | 確認登入狀態 | — |

---

## 資料庫 Migration 管理

新增欄位或修改資料表時：

```bash
# 新增 migration
dotnet ef migrations add <MigrationName> \
  --project src/TCS.Infrastructure \
  --startup-project src/TCS.Web

# 套用到資料庫
dotnet ef database update \
  --project src/TCS.Infrastructure \
  --startup-project src/TCS.Web
```

---

## 建置與測試

```bash
# 建置
dotnet build

# 執行測試
dotnet test
```
