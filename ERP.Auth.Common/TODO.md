# ERP.Auth.Common — 待辦事項

## ✅ 已完成

### Dashboard 監控（含帳密保護）
- `ERP.Gateway` 新增 `/dashboard` 狀態頁面
- Cookie 驗證（帳號/密碼，8 小時 session）
- 從 YARP Cluster 設定自動推導健康檢查 URL（不重複設定 port）
- 密碼可透過環境變數 `Dashboard__Password` 覆蓋（不需改 appsettings）
- `health-check.ps1` 健康監控腳本（可加入 Windows 工作排程器）

### R2-B: 確認 Jwt:AllowedOrigins 設定 — Done
- `MapErpAuthEndpoints()` 已強制要求 `Jwt:AllowedOrigins` 不可為空，未設定時直接啟動失敗。
- `/api/auth/token-login` 已用 `Origin` header 做白名單檢查，不在清單內會回傳 `403 Forbidden`。
- 現有整合子系統已驗證補上 erp_plus 實際 Origin，包含 port：
  - AccountCodeCreate：`src/AccountCode.Api/appsettings.json`
  - AccountOffsetEntrySystem：`appsettings.Security.json`
  - AssetManagementSystem：`AssetCRUD/appsettings.json`
  - BankAccountCreate：`DbnetsuiteCore/appsettings.json`
  - SupplierCreate：`src/DingxinErp.Web/appsettings.json`
- 目前 staging / production erp_plus Origin：
  - `http://192.168.168.15:9922`（staging erp_plus）
  - `http://192.168.168.15:9911`（production erp_plus）
  - `http://localhost:3513`、`http://127.0.0.1:3513`（本機開發）
- 已更新團隊整合文件，提醒 Origin 必須填完整 `scheme://host:port`，不可只填 host。

---

## 🔴 高優先（安全相關）

目前沒有高優先未完成項目。

---

## 🟡 中優先（API 設計）

### R2-G: 多值 Claim 用逗號合併，語意不明確
- **現狀**：`Claims["role"]` 在多個 role 時回傳 `"admin,finance"`（逗號合併字串）
- **問題**：
  - `role = ["admin,finance"]`（role 名稱含逗號）與 `["admin", "finance"]` 結果相同，無法區分
  - 消費端若用 `== "admin"` 判斷會意外失效
- **建議修改**：將 `JwtValidationResult.Claims` 型別從 `IReadOnlyDictionary<string, string>`  
  改為 `IReadOnlyDictionary<string, IReadOnlyList<string>>`
- **代價**：Breaking change，需同時更新：
  - `JwtValidationResult.cs`（工廠方法 + 型別）
  - `JwtService.cs`（GroupBy 建構方式）
  - `JwtServiceTests.cs`（相關斷言）
  - 各整合專案的消費端程式碼
- **建議時機**：整合第一個專案前處理，代價最低

---

## 🟢 低優先（基礎設施）

### R1-13: Rate Limiting
- `/api/auth/token-login` 無限流保護
- **建議**：由整合方在 nginx / ASP.NET Core Rate Limiting middleware 設定
- Library 層不加（避免綁定特定實作策略）
- 參考文件：https://learn.microsoft.com/aspnet/core/performance/rate-limit
