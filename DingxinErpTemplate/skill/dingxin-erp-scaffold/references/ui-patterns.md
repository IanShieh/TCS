# 前端 UI 模式 — 鼎新 ERP 作業轉換

## 頁面結構 (傳統 Web Form 式)

★ 單頭和單身 CRUD 完全分離，各自擁有獨立的 Modal 和操作按鈕

```
┌─────────────────────────────────────────────────────────┐
│ [搜尋列] 搜尋框 + 搜尋/清除按鈕 + 單頭 新增/編輯/刪除按鈕  │
├─────────────────────────────────────────────────────────┤
│ [單頭表格] checkbox + 欄位 + 狀態 Badge + 單身筆數        │
│ ├ 行1 ☐ TA001 TA002 TA003 ...                          │
│ ├ 行2 ☐ ...                                            │
│ └ 分頁: « 1 2 3 » 共 N 筆                               │
├─────────────────────────────────────────────────────────┤
│ [單身表格] (點擊單頭行後自動選取該行並顯示)                    │
│ ├ 標題: 單身明細 — 單別: XX / 單號: YY  [新增明細][編輯明細][刪除明細] │
│ ├ ☐ 序號 品號 品名 數量 單價 金額 備註                      │
│ └ ...                                                   │
└─────────────────────────────────────────────────────────┘

[Modal] #headerModal — 單頭新增/編輯 (僅單頭欄位，modal-lg)
[Modal] #detailModal — 單身新增/編輯 (單筆明細欄位)
[Modal] #deleteHeaderModal — 單頭刪除確認
[Modal] #deleteDetailModal — 單身刪除確認
[Toast] 操作結果通知 (右下角)
```

## HTML 結構範本

### 搜尋區

```html
<div class="card mb-3">
  <div class="card-body py-2">
    <div class="row align-items-center">
      <div class="col-auto"><h5>[作業名稱]</h5></div>
      <div class="col">
        <div class="input-group">
          <input type="text" id="searchInput" class="form-control form-control-sm" placeholder="搜尋..." />
          <button class="btn btn-outline-primary btn-sm" id="btnSearch"><i class="bi bi-search"></i> 搜尋</button>
          <button class="btn btn-outline-secondary btn-sm" id="btnClearSearch"><i class="bi bi-x-circle"></i></button>
        </div>
      </div>
      <div class="col-auto">
        <button class="btn btn-success btn-sm" id="btnCreate"><i class="bi bi-plus-lg"></i> 新增</button>
        <button class="btn btn-warning btn-sm" id="btnEdit" disabled><i class="bi bi-pencil"></i> 編輯</button>
        <button class="btn btn-danger btn-sm" id="btnDelete" disabled><i class="bi bi-trash"></i> 刪除</button>
      </div>
    </div>
  </div>
</div>
```

### 表格 (Thead 需依欄位調整)

```html
<table class="table table-striped table-hover table-sm" id="headerTable">
  <thead class="table-primary">
    <tr>
      <th><input type="checkbox" id="checkAll" /></th>
      <th>[欄位1]</th>
      <th>[欄位2]</th>
      ...
    </tr>
  </thead>
  <tbody id="headerTableBody"></tbody>
</table>
```

### 單頭 Modal (僅單頭欄位)

```html
<div class="modal fade" id="headerModal" tabindex="-1">
  <div class="modal-dialog modal-lg">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title" id="headerModalTitle">新增</h5>
      </div>
      <div class="modal-body">
        <form id="headerForm">
          <input type="hidden" id="formMode" value="create" />
          <!-- 僅單頭欄位，不含單身 -->
          <div class="row g-3 mb-3">
            <div class="col-md-3">
              <label class="form-label">[欄位名] <span class="text-danger">*</span></label>
              <input type="text" class="form-control" id="f_[欄位]" maxlength="[長度]" required />
            </div>
            ...
          </div>
        </form>
      </div>
      <div class="modal-footer">
        <button class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
        <button class="btn btn-primary" id="btnSave"><i class="bi bi-check-lg"></i> 儲存</button>
      </div>
    </div>
  </div>
</div>
```

### 單身 Modal (單筆明細)

```html
<div class="modal fade" id="detailModal" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title" id="detailModalTitle">新增明細</h5>
      </div>
      <div class="modal-body">
        <form id="detailForm">
          <input type="hidden" id="detailFormMode" value="create" />
          <div class="row g-3 mb-3">
            <div class="col-md-4">
              <label class="form-label">序號 <span class="text-danger">*</span></label>
              <input type="text" class="form-control" id="fd_TB003" maxlength="4" required />
            </div>
            <div class="col-md-4">
              <label class="form-label">品號 <span class="text-danger">*</span></label>
              <input type="text" class="form-control" id="fd_TB004" />
            </div>
            ...
          </div>
        </form>
      </div>
      <div class="modal-footer">
        <button class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
        <button class="btn btn-primary" id="btnSaveDetail"><i class="bi bi-check-lg"></i> 儲存</button>
      </div>
    </div>
  </div>
</div>
```

### 單身區域 (含獨立 CRUD 按鈕)

```html
<div class="card mb-3" id="detailCard" style="display:none;">
  <div class="card-header py-2 bg-info bg-opacity-10">
    <strong><i class="bi bi-list-nested"></i> 單身明細</strong>
    <span id="detailInfo" class="ms-2 text-muted"></span>
    <span id="detailCount" class="badge bg-info ms-2">0 筆</span>
    <div class="float-end">
      <button class="btn btn-outline-success btn-sm" id="btnCreateDetail">
        <i class="bi bi-plus"></i> 新增明細
      </button>
      <button class="btn btn-outline-warning btn-sm" id="btnEditDetail" disabled>
        <i class="bi bi-pencil"></i> 編輯明細
      </button>
      <button class="btn btn-outline-danger btn-sm" id="btnDeleteDetail" disabled>
        <i class="bi bi-trash"></i> 刪除明細
      </button>
    </div>
  </div>
  <table class="table table-striped table-hover table-sm mb-0">
    <thead class="table-info">
      <tr>
        <th><input type="checkbox" id="detailCheckAll" /></th>
        <th>序號</th>
        <th>[欄位]</th>
        ...
      </tr>
    </thead>
    <tbody id="detailTableBody"></tbody>
  </table>
</div>
```

## JavaScript 模式

### 通用函式 (crud-common.js — 僅處理單頭 CRUD)

- `showToast(message, type)` — Toast 通知
- `renderPagination(containerId, pagedResult, onPageClick)` — 分頁
- `loadHeaders(page, search)` — 載入單頭 (需設定全域 `API_BASE`)
- `initCrudButtons(apiBase)` — 初始化單頭 CRUD 按鈕事件
- `openHeaderModal(mode, pk1, pk2)` — 開啟單頭新增/編輯 Modal
- `saveHeaderForm(apiBase)` — 儲存單頭 (不含單身)
- `initKeyboardShortcuts()` — 鍵盤快捷鍵

### 單身連動函式 (master-detail.js — 獨立單身 CRUD)

- `initMasterDetail(apiBase)` — 初始化單頭↔單身連動 + 單身 CRUD 按鈕
  - 點擊單頭行 → 自動勾選該行 checkbox + 啟用編輯/刪除按鈕 + 載入單身
  - 切換行時自動取消前一行選取（單選模式）
  - 點擊單身行 → 自動勾選該行 checkbox
- `loadDetails(apiBase, pk1, pk2)` — 載入單身明細
- `openDetailModal(mode, seq)` — 開啟單身新增/編輯 Modal
- `saveDetailForm(apiBase)` — 儲存單筆單身
- `updateDetailButtonState()` — 更新單身按鈕狀態

### 頁面初始化 (每個作業頁面的 @section Scripts)

```javascript
const API_BASE = '/api/[Entity]';
const PAGE_SIZE = 10;
let currentPage = 1;
let selectedRows = [];

$(document).ready(function () {
    loadHeaders(1);
    initCrudButtons(API_BASE);
    initMasterDetail(API_BASE);
    initKeyboardShortcuts();
});
```
