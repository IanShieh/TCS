# Release Note

- 日期：2026-04-24
- Commit：46c6c9a
- 標題：新增 Gateway Dashboard 與 Aspire 監控

## 概要

本次更新強化 ERP.Auth.Common 的 Gateway 管理能力與 Aspire 維運支援，新增 Dashboard 登入與服務狀態頁，並補齊多子系統在 AppHost、YARP、健康檢查、啟停監控與 staging 部署上的整體操作流程。

## 重點更新

- 新增 Gateway Dashboard，提供登入保護與服務健康狀態總覽。
- 將 Dashboard 管理介面的 Cookie 驗證與 ERP API 的 JWT 驗證分流，降低路由與授權混用風險。
- 固定 Gateway 對外入口為 8180，統一路由入口與部署行為。
- 調整未授權導向流程，子系統在 session 失效時會回到各自的 session-expired 頁面，而非直接導回根路徑。
- 強化健康檢查安全性，/health 與 /alive 僅允許 localhost 呼叫。
- 更新前端 erp-auth.js，依 PathBase 正確處理未授權跳轉。
- 新增 Aspire watchdog、停止腳本、staging 部署腳本與整合文件，提升啟停、監控與交接效率。

## 主要影響

- 管理者可透過 Dashboard 集中查看各 ERP 子系統狀態。
- Gateway 路由規則更清楚區分匿名頁面、登入入口與受 JWT 保護的 API。
- 本機與 staging 環境的啟動、監控、部署流程更一致，可維運性提升。

## 涉及檔案

- ERP.AppHost/Program.cs
- ERP.Gateway/DashboardHtml.cs
- ERP.Gateway/Program.cs
- ERP.Gateway/appsettings.json
- ERP.Gateway/appsettings.Staging.json
- ERP.ServiceDefaults/Extensions.cs
- Extensions/ErpAuthExtensions.cs
- wwwroot/js/erp-auth.js
- aspire-watchdog.ps1
- deploy-staging.ps1
- stop-aspire.ps1
- start.cmd
- docs/aspire-integration-guide.md
- TODO.md

## 驗證結果

- dotnet build ERP.Auth.Common.sln 成功
- dotnet test ERP.Auth.Common.Tests/ERP.Auth.Common.Tests.csproj 通過
- 測試結果：27 通過，0 失敗，0 跳過