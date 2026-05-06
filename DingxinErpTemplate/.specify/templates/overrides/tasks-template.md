# 任務清單: [FEATURE_NAME]

<!--
  任務格式：- [ ] T001 [P] [US1] 描述，包含精確檔案路徑
  [P]  = 可平行執行（不同檔案且無前置依賴）
  [US1] = Phase 3+ 任務標記（整個 ERP 作業 = 一個 User Story）
-->

## Phase 1: 基礎設定

- [ ] T001 [P] 建立 Entity 類別 (單頭 + 單身) — `src/[Proj].Core/Entities/`
- [ ] T002 [P] 建立 DbContext Configuration (IsFixedLength) — `src/[Proj].Infrastructure/Configurations/`
- [ ] T003 [P] 建立 DTOs (HeaderDto, DetailDto, CreateRequest, UpdateRequest, MappingExtensions) — `src/[Proj].Core/DTOs/`

## Phase 2: 業務邏輯

- [ ] T004 建立 IRepository + Repository — `Core/Interfaces/` + `Infrastructure/Repositories/`
- [ ] T005 建立 IService + Service — `Core/Interfaces/` + `Core/Services/`
- [ ] T006 建立 FluentValidation Validators — `src/[Proj].Core/Validators/`

## Phase 3: API + 前端

- [ ] T007 [US1] 建立 API Controller (CRUD + 搜尋 + 單身) — `src/[Proj].Web/Controllers/`
- [ ] T008 [P] [US1] 建立 MVC 頁面 Controller — `src/[Proj].Web/Controllers/`
- [ ] T009 [US1] 建立 Index.cshtml (搜尋 + 表格 + Modal + 單身區) — `src/[Proj].Web/Views/`
- [ ] T010 [P] [US1] 調整 _Layout.cshtml 導覽列 — `src/[Proj].Web/Views/Shared/`

## Phase 4: 整合 + 測試

- [ ] T011 更新 DI 註冊 (Program.cs) — `src/[Proj].Web/Program.cs`
- [ ] T012 [P] 建立單元測試 (Service + Validator) — `tests/[Proj].Core.Tests/`
- [ ] T013 整合測試 (Build + 手動驗證)

## 備註

- [P] = 可平行執行
- 每個 Task 完成後執行 `dotnet build` 確認編譯通過
- 影響同一檔案的相關 Task 必須循序執行
