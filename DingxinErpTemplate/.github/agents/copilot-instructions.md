# DingxinErpTemplate Development Guidelines

> 鼎新 ERP 作業轉換模板 — .NET 8 Clean Architecture

## Active Technologies

- .NET 8 LTS, EF Core 8, FluentValidation 11, xUnit + Moq
- Bootstrap 5 + jQuery (CDN), Swagger (開發環境)
- SQL Server 2008 R2 相容 (TLS 1.0, ROW_NUMBER() 分頁)

## Project Structure

```text
demo/          # 範例模板 (唯讀參考，scaffold 複製來源)
src/           # 開發區 (scaffold 後為作業專案)
tests/         # 單元測試
specs/         # SDD 規格文件
ops-docs/      # ERP 作業原始文件
```

## Architecture Rules

- 三層架構: Web → Core ← Infrastructure (Core 無外部依賴)
- 統一回傳: CrudResult<T> / PagedResult<T>
- 手動映射 (MappingExtensions)，不使用 AutoMapper
- char 欄位必須 IsFixedLength() + IsUnicode(false)
- 雙路徑分頁: InMemory → LINQ Skip/Take, SQL Server → ROW_NUMBER()
- USE_INMEMORY_DB 環境變數切換資料庫模式
- 分頁 UI 最多 5 頁 + 省略號

## Security

- gitleaks pre-commit hook: auto-enabled on first `dotnet build` (via Directory.Build.props + .githooks/)
- CI: .github/workflows/security-scan.yml scans push/PR to main
- Prerequisite: `winget install gitleaks`

## Commands

```bash
dotnet build                                    # 建置 demo (首次自動啟用 gitleaks hook)
dotnet run --project demo/src/DingxinErp.Web    # 執行 demo
dotnet run --project src/DingxinErp.Web         # 執行開發中專案
dotnet test                                     # 測試
```

## Team Workflow

```
1. 從 initial-template 切出 feature branch (本機)
2. SDD: scaffold → specify → plan → tasks → implement
3. 開發完成 → 選擇匯出方式:
   A. export-and-push.ps1 → GitHub Repo + push
   B. export-bundle.ps1  → 本機 .bundle 封存
4. 刪除本機 feature branch
```

兩種匯出都會自動: 移除 demo/、skill/、_example、產生專案 README

遠端只保留 main 和 initial-template

## Code Style

- Entity 屬性名用 ERP 原始欄位名 (MA001, TA001)
- 類別名/方法名英文，註解/文件繁體中文
- async/await 非同步模式
- JSON 序列化 PropertyNamingPolicy = null (PascalCase)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
