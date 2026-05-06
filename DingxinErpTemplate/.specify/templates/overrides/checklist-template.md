# 驗收清單: [FEATURE_NAME]

<!--
  雙用途清單：
  A. 規格品質驗證 (pre-implementation) — 用問句檢查需求書寫品質
  B. ERP 實作架構核對 (post-implementation) — CHK001+ 檢查程式碼符合標準
-->

**功能:** [來自 spec.md]
**日期:** [DATE]

## 架構檢核

- [ ] CHK001 Clean Architecture 三層分離
- [ ] CHK002 Entity 使用 ERP 原始欄位名
- [ ] CHK003 char 欄位使用 IsFixedLength() + IsUnicode(false)
- [ ] CHK004 CrudResult<T> 統一回傳
- [ ] CHK005 IAuditableEntity 審計欄位

## 功能檢核

- [ ] CHK006 新增功能正常
- [ ] CHK007 編輯功能正常
- [ ] CHK008 刪除功能正常 (含 Cascade 單身)
- [ ] CHK009 搜尋功能正常
- [ ] CHK010 分頁功能正常
- [ ] CHK011 單頭單身連動正常
- [ ] CHK012 FluentValidation 驗證正常

## UI 檢核

- [ ] CHK013 傳統 Web Form 式佈局
- [ ] CHK014 Toast 通知正常
- [ ] CHK015 鍵盤快捷鍵正常
- [ ] CHK016 響應式設計 (手機/平板)

## 程式碼品質

- [ ] CHK017 dotnet build 無錯誤
- [ ] CHK018 dotnet test 全部通過
- [ ] CHK019 Swagger API 文件正確
- [ ] CHK020 繁體中文文件 + 英文程式碼
