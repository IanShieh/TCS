# Spec-kit SDD 設定指引 — 鼎新 ERP 作業轉換

## 初始化步驟

### 1. 建立規格資料夾

```powershell
.\.specify\scripts\powershell\create-new-feature.ps1 -FeatureNumber "001" -FeatureName "[功能名]"
```

這會建立 `specs/001-[功能名]/` 並複製 4 個模板檔案。

### 2. 填寫規格書 (spec.md)

關鍵區塊：

- **作業名稱**: 中文 + 英文
- **表格資訊**: 單頭表格名、單身表格名、PK/FK
- **欄位清單**: 每欄的 名稱/型態/長度/必填/說明
- **使用者情境**: CRUD + 搜尋 + 單頭單身連動
- **特殊規則**: 計算邏輯、下拉選單來源、驗證規則

### 3. SDD 流程

| 步驟 | 指令 | 產出 |
|------|------|------|
| 憲法確認 | `/speckit.constitution` | 確認團隊規範 (EN: `constitution.md`) |
| 需求定義 | `/speckit.specify` | `specs/NNN/spec.md` |
| 技術計畫 | `/speckit.plan` | `specs/NNN/plan.md` |
| 任務清單 | `/speckit.tasks` | `specs/NNN/tasks.md` |
| 逐步實作 | `/speckit.implement` | 程式碼檔案 |
| 驗收檢核 | `/speckit.checklist` | `specs/NNN/checklist.md` |

## 憲法要點 (constitution.md)

1. **Clean Architecture 三層架構** — Web/Core/Infrastructure
2. **繁體中文文件 + 英文程式碼**
3. **CrudResult<T> 統一回傳**
4. **IAuditableEntity 審計欄位**
5. **char 欄位 IsFixedLength()** — 鼎新 ERP 相容
6. **傳統 Web Form 式 UI** — 搜尋+表格+Modal

## 每個新專案需要的 SDD 檔案

模板已包含以下檔案，複製模板時會自動帶入：

```
.specify/
├── memory/constitution.md      ← 團隊憲法 英文版 (供 spec-kit 自動化使用)
├── memory/constitution-zh-TW.md← 團隊憲法 中文版 (供團隊閱讀參考)
├── templates/                  ← 模板 (不需修改)
└── scripts/powershell/         ← 腳本 (不需修改)

.github/
├── prompts/speckit.*.prompt.md ← AI 提示 (不需修改)
└── agents/speckit.*.agent.md   ← Agent 定義 (不需修改)

.claude/commands/speckit.*.md   ← Claude 指令 (不需修改)

specs/                          ← 功能規格存放處 (每個功能一個資料夾)
```
