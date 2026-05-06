/**
 * crud-common.js — 通用單頭 CRUD 功能 (搜尋/分頁/Modal/Toast/鍵盤快捷鍵)
 * ★ 此檔案為模板共用 JS，僅處理單頭 CRUD，單身由 master-detail.js 處理
 */

// ===== Toast 通知 =====
function showToast(message, type = 'success') {
    const bgClass = type === 'success' ? 'bg-success' : type === 'danger' ? 'bg-danger' : 'bg-warning';
    const icon = type === 'success' ? 'check-circle' : type === 'danger' ? 'x-circle' : 'exclamation-triangle';
    const html = `
        <div class="toast align-items-center text-white ${bgClass} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-${icon}"></i> ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;
    const container = document.getElementById('toastContainer');
    container.insertAdjacentHTML('beforeend', html);
    const toastEl = container.lastElementChild;
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

// ===== 分頁渲染 (最多顯示 5 頁 + 省略號) =====
function renderPagination(containerId, pagedResult, onPageClick) {
    const container = document.getElementById(containerId);
    if (!container || pagedResult.TotalPages <= 1) {
        if (container) container.innerHTML = '';
        return;
    }

    const maxVisible = 5;
    let startPage = Math.max(1, pagedResult.CurrentPage - Math.floor(maxVisible / 2));
    let endPage = Math.min(pagedResult.TotalPages, startPage + maxVisible - 1);
    if (endPage - startPage < maxVisible - 1) startPage = Math.max(1, endPage - maxVisible + 1);

    let html = '<nav><ul class="pagination pagination-sm mb-0 justify-content-center">';
    html += `<li class="page-item ${pagedResult.CurrentPage <= 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${pagedResult.CurrentPage - 1}">&laquo;</a></li>`;

    if (startPage > 1) {
        html += `<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>`;
        if (startPage > 2) html += `<li class="page-item disabled"><span class="page-link">\u2026</span></li>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<li class="page-item ${i === pagedResult.CurrentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a></li>`;
    }

    if (endPage < pagedResult.TotalPages) {
        if (endPage < pagedResult.TotalPages - 1) html += `<li class="page-item disabled"><span class="page-link">\u2026</span></li>`;
        html += `<li class="page-item"><a class="page-link" href="#" data-page="${pagedResult.TotalPages}">${pagedResult.TotalPages}</a></li>`;
    }

    html += `<li class="page-item ${pagedResult.CurrentPage >= pagedResult.TotalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${pagedResult.CurrentPage + 1}">&raquo;</a></li>`;
    html += '</ul></nav>';
    html += `<div class="text-center mt-1 small text-muted">共 ${pagedResult.TotalItems} 筆，第 ${pagedResult.CurrentPage} / ${pagedResult.TotalPages} 頁</div>`;

    container.innerHTML = html;
    container.querySelectorAll('.page-link').forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const page = parseInt(this.dataset.page);
            if (page >= 1 && page <= pagedResult.TotalPages) {
                onPageClick(page);
            }
        });
    });
}

// ===== 排序狀態 =====
var currentSortBy = '', currentSortDir = 'asc';

// ===== 單頭表格載入 =====
function loadHeaders(page, search) {
    const params = new URLSearchParams({ page: page, pageSize: PAGE_SIZE });
    if (search) params.set('search', search);
    if (currentSortBy) params.set('sortBy', currentSortBy);
    if (currentSortDir) params.set('sortDir', currentSortDir);

    $.ajax({
        url: `${API_BASE}?${params}`,
        method: 'GET',
        success: function (data) {
            currentPage = data.CurrentPage;
            renderHeaderTable(data.Items);
            updateSortIcons('headerTable');
            renderPagination('paginationContainer', data, loadHeaders);
            $('#headerCount').text(`${data.TotalItems} 筆`);
            clearSelection();
            $('#detailCard').hide();
        },
        error: function () {
            showToast('載入資料失敗', 'danger');
        }
    });
}

// ===== 渲染單頭表格 =====
function renderHeaderTable(items) {
    const tbody = document.getElementById('headerTableBody');
    if (!items || items.length === 0) {
        tbody.innerHTML = `<tr id="emptyRow">
            <td colspan="8" class="text-center text-muted py-4">
                <i class="bi bi-inbox display-6"></i><p class="mt-2">尚無資料</p>
            </td></tr>`;
        return;
    }

    tbody.innerHTML = items.map(item => `
        <tr data-ta001="${item.TA001}" data-ta002="${item.TA002}" class="cursor-pointer">
            <td><input type="checkbox" class="row-check" value="${item.TA001}|${item.TA002}" /></td>
            <td>${item.TA001}</td>
            <td>${item.TA002}</td>
            <td>${item.TA003}</td>
            <td>${item.TA004}</td>
            <td>${item.TA005 || ''}</td>
            <td><span class="badge ${item.TA007 === 'Y' ? 'bg-success' : 'bg-secondary'}">${item.TA007}</span></td>
            <td><span class="badge bg-info">${item.DetailCount}</span></td>
        </tr>
    `).join('');
}

// ===== 選取管理 =====
function clearSelection() {
    selectedRows = [];
    $('.row-check').prop('checked', false);
    $('#checkAll').prop('checked', false);
    updateButtonState();
}

function updateButtonState() {
    $('#btnEdit').prop('disabled', selectedRows.length !== 1);
    $('#btnDelete').prop('disabled', selectedRows.length === 0);
}

function getSelectedKeys() {
    return selectedRows.map(val => {
        const [ta001, ta002] = val.split('|');
        return { ta001, ta002 };
    });
}

// ===== CRUD 按鈕初始化 (僅單頭) =====
function initCrudButtons(apiBase) {
    // 全選
    $(document).on('change', '#checkAll', function () {
        const checked = $(this).prop('checked');
        $('.row-check').prop('checked', checked);
        selectedRows = checked ? $('.row-check').map(function () { return $(this).val(); }).get() : [];
        updateButtonState();
    });

    // 單選
    $(document).on('change', '.row-check', function () {
        const val = $(this).val();
        if ($(this).prop('checked')) {
            if (!selectedRows.includes(val)) selectedRows.push(val);
        } else {
            selectedRows = selectedRows.filter(v => v !== val);
        }
        updateButtonState();
    });

    // 搜尋
    $('#btnSearch').click(function () {
        loadHeaders(1, $('#searchInput').val().trim());
    });
    $('#searchInput').keypress(function (e) {
        if (e.which === 13) $('#btnSearch').click();
    });
    $('#btnClearSearch').click(function () {
        $('#searchInput').val('');
        loadHeaders(1);
    });

    // 新增
    $('#btnCreate').click(function () {
        openHeaderModal('create');
    });

    // 編輯
    $('#btnEdit').click(function () {
        if (selectedRows.length !== 1) return;
        const keys = getSelectedKeys()[0];
        openHeaderModal('edit', keys.ta001, keys.ta002);
    });

    // 刪除 — 顯示單頭刪除確認
    $('#btnDelete').click(function () {
        if (selectedRows.length === 0) return;
        $('#deleteCount').text(selectedRows.length);
        new bootstrap.Modal('#deleteHeaderModal').show();
    });

    // 確認刪除單頭
    $('#btnConfirmDelete').click(async function () {
        const keys = getSelectedKeys();
        let successCount = 0;
        for (const key of keys) {
            try {
                await $.ajax({ url: `${apiBase}/${key.ta001}/${key.ta002}`, method: 'DELETE' });
                successCount++;
            } catch (e) { /* skip */ }
        }
        bootstrap.Modal.getInstance('#deleteHeaderModal').hide();
        showToast(`成功刪除 ${successCount} 筆資料`);
        loadHeaders(currentPage, $('#searchInput').val().trim());
    });

    // 儲存單頭
    $('#btnSave').click(function () {
        saveHeaderForm(apiBase);
    });
}

// ===== 單頭 Modal 開啟 =====
function openHeaderModal(mode, ta001, ta002) {
    $('#formMode').val(mode);

    if (mode === 'create') {
        $('#headerModalTitle').text('新增');
        $('#headerForm')[0].reset();
        $('#f_TA001, #f_TA002').prop('readonly', false);
    } else {
        $('#headerModalTitle').text('編輯');
        $('#f_TA001, #f_TA002').prop('readonly', true);
        $.ajax({
            url: `${API_BASE}/${ta001}/${ta002}`,
            method: 'GET',
            success: function (result) {
                if (result.Success) {
                    const d = result.Data;
                    $('#f_TA001').val(d.TA001);
                    $('#f_TA002').val(d.TA002);
                    $('#f_TA003').val(d.TA003);
                    $('#f_TA004').val(d.TA004);
                    $('#f_TA005').val(d.TA005 || '');
                    $('#f_TA006').val(d.TA006 || '');
                    $('#f_TA007').val(d.TA007);
                }
            }
        });
    }

    new bootstrap.Modal('#headerModal').show();
}

// ===== 儲存單頭 (僅單頭欄位，不含單身) =====
function saveHeaderForm(apiBase) {
    const mode = $('#formMode').val();
    const ta001 = $('#f_TA001').val().trim();
    const ta002 = $('#f_TA002').val().trim();

    let payload;
    let url;
    let method;

    if (mode === 'create') {
        payload = {
            TA001: ta001,
            TA002: ta002,
            TA003: $('#f_TA003').val().trim(),
            TA004: $('#f_TA004').val().trim(),
            TA005: $('#f_TA005').val().trim(),
            TA006: $('#f_TA006').val().trim(),
            TA007: $('#f_TA007').val()
        };
        url = apiBase;
        method = 'POST';
    } else {
        payload = {
            TA003: $('#f_TA003').val().trim(),
            TA004: $('#f_TA004').val().trim(),
            TA005: $('#f_TA005').val().trim(),
            TA006: $('#f_TA006').val().trim(),
            TA007: $('#f_TA007').val()
        };
        url = `${apiBase}/${ta001}/${ta002}`;
        method = 'PUT';
    }

    $.ajax({
        url: url,
        method: method,
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (result) {
            if (result.Success) {
                bootstrap.Modal.getInstance('#headerModal').hide();
                showToast(result.Message);
                loadHeaders(currentPage, $('#searchInput').val().trim());
            } else {
                showToast(result.Message || '操作失敗', 'danger');
            }
        },
        error: function (xhr) {
            const errMsg = xhr.responseJSON?.Message || xhr.responseJSON?.title || '操作失敗';
            const errors = xhr.responseJSON?.Errors;
            if (errors && typeof errors === 'object') {
                const msgs = Object.values(errors).flat();
                showToast(msgs.join('、'), 'danger');
            } else {
                showToast(errMsg, 'danger');
            }
        }
    });
}

// ===== 排序 =====
function initSortable(tableId) {
    $(`#${tableId}`).on('click', 'th.sortable', function () {
        const col = $(this).data('sort');
        if (currentSortBy === col) {
            currentSortDir = currentSortDir === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortBy = col;
            currentSortDir = 'asc';
        }
        currentPage = 1;
        loadHeaders(currentPage, $('#searchInput').val().trim());
    });
}

function updateSortIcons(tableId) {
    $(`#${tableId} th.sortable .sort-icon`).attr('class', 'bi bi-arrow-down-up text-muted sort-icon');
    const $th = $(`#${tableId} th.sortable[data-sort="${currentSortBy}"]`);
    const iconClass = currentSortDir === 'asc' ? 'bi bi-sort-alpha-down text-primary sort-icon' : 'bi bi-sort-alpha-up text-primary sort-icon';
    $th.find('.sort-icon').attr('class', iconClass);
}

// ===== 鍵盤快捷鍵 =====
function initKeyboardShortcuts() {
    $(document).keydown(function (e) {
        // Ctrl+N: 新增
        if (e.ctrlKey && e.key === 'n') { e.preventDefault(); $('#btnCreate').click(); }
        // Ctrl+E: 編輯
        if (e.ctrlKey && e.key === 'e') { e.preventDefault(); $('#btnEdit').click(); }
        // Delete: 刪除
        if (e.key === 'Delete' && !$(e.target).is('input, textarea, select')) { $('#btnDelete').click(); }
        // F5: 重新整理
        if (e.key === 'F5') { e.preventDefault(); loadHeaders(currentPage, $('#searchInput').val().trim()); }
        // Escape: 關閉 Modal
        if (e.key === 'Escape') { $('.modal.show').modal('hide'); }
    });
}
