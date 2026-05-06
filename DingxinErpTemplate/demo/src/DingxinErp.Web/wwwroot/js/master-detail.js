/**
 * master-detail.js — 單頭單身連動 + 單身獨立 CRUD
 * ★ 此檔案為模板共用 JS，處理單頭↔單身連動及單身的新增/編輯/刪除
 */

// 目前選取的單頭主鍵
let currentHeaderTA001 = null;
let currentHeaderTA002 = null;

// 目前勾選的單身序號清單
let selectedDetailRows = [];

function initMasterDetail(apiBase) {
    // 點擊單頭行 → 自動選取該行 + 載入單身明細
    $(document).on('click', '#headerTableBody tr:not(#emptyRow)', function (e) {
        if ($(e.target).is('input[type="checkbox"]')) return;

        const ta001 = $(this).data('ta001');
        const ta002 = $(this).data('ta002');
        if (!ta001 || !ta002) return;

        // 高亮選取行
        $('#headerTableBody tr').removeClass('selected-row');
        $(this).addClass('selected-row');

        // 自動選取該行 checkbox（單選模式）
        $('.row-check').prop('checked', false);
        const cb = $(this).find('.row-check');
        cb.prop('checked', true);
        selectedRows = [cb.val()];
        $('#checkAll').prop('checked', false);
        updateButtonState();

        // 記錄目前單頭
        currentHeaderTA001 = String(ta001);
        currentHeaderTA002 = String(ta002);

        // 啟用單身新增按鈕
        $('#btnCreateDetail').prop('disabled', false);

        // 載入單身
        loadDetails(apiBase, currentHeaderTA001, currentHeaderTA002);
    });

    // ===== 單身勾選 =====
    $(document).on('change', '.detail-row-check', function () {
        const val = $(this).val();
        if ($(this).prop('checked')) {
            if (!selectedDetailRows.includes(val)) selectedDetailRows.push(val);
        } else {
            selectedDetailRows = selectedDetailRows.filter(v => v !== val);
        }
        updateDetailButtonState();
    });

    $(document).on('change', '#detailCheckAll', function () {
        const checked = $(this).prop('checked');
        $('.detail-row-check').prop('checked', checked);
        selectedDetailRows = checked
            ? $('.detail-row-check').map(function () { return $(this).val(); }).get()
            : [];
        updateDetailButtonState();
    });

    // 點擊單身列也可觸發勾選
    $(document).on('click', '#detailTableBody tr', function (e) {
        if ($(e.target).is('input[type="checkbox"]')) return;
        const cb = $(this).find('.detail-row-check');
        if (cb.length) {
            cb.prop('checked', !cb.prop('checked')).trigger('change');
        }
    });

    // ===== 單身 CRUD 按鈕 =====
    $('#btnCreateDetail').click(function () {
        if (!currentHeaderTA001) return;
        openDetailModal(apiBase, 'create');
    });

    $('#btnEditDetail').click(function () {
        if (selectedDetailRows.length !== 1) return;
        openDetailModal(apiBase, 'edit', selectedDetailRows[0]);
    });

    $('#btnDeleteDetail').click(function () {
        if (selectedDetailRows.length === 0) return;
        $('#deleteDetailCount').text(selectedDetailRows.length);
        new bootstrap.Modal('#deleteDetailModal').show();
    });

    // 確認刪除單身
    $('#btnConfirmDeleteDetail').click(async function () {
        let successCount = 0;
        for (const tb003 of selectedDetailRows) {
            try {
                await $.ajax({
                    url: `${apiBase}/${currentHeaderTA001}/${currentHeaderTA002}/details/${tb003}`,
                    method: 'DELETE'
                });
                successCount++;
            } catch (e) { /* skip */ }
        }
        bootstrap.Modal.getInstance('#deleteDetailModal').hide();
        showToast(`成功刪除 ${successCount} 筆明細`);
        clearDetailSelection();
        loadDetails(apiBase, currentHeaderTA001, currentHeaderTA002);
        // 重新整理單頭以更新 DetailCount
        loadHeaders(currentPage, $('#searchInput').val().trim());
    });

    // 儲存單身
    $('#btnSaveDetail').click(function () {
        saveDetailForm(apiBase);
    });

    // 自動計算金額 = 數量 × 單價
    $(document).on('change input', '#fd_TB006, #fd_TB007', function () {
        const qty = parseFloat($('#fd_TB006').val()) || 0;
        const price = parseFloat($('#fd_TB007').val()) || 0;
        $('#fd_TB008').val((qty * price).toFixed(4));
    });
}

// ===== 載入單身資料 =====
function loadDetails(apiBase, ta001, ta002) {
    $('#detailCard').show();
    $('#detailInfo').text(`— 單別: ${ta001} / 單號: ${ta002}`);
    $('#detailTableBody').html('<tr><td colspan="8" class="text-center py-3"><i class="bi bi-hourglass-split"></i> 載入中...</td></tr>');

    $.ajax({
        url: `${apiBase}/${ta001}/${ta002}/details`,
        method: 'GET',
        success: function (result) {
            if (result.Success) {
                renderDetailTable(result.Data);
                $('#detailCount').text(`${result.Data.length} 筆`);
            } else {
                $('#detailTableBody').html('<tr><td colspan="8" class="text-center text-danger">載入失敗</td></tr>');
            }
        },
        error: function () {
            $('#detailTableBody').html('<tr><td colspan="8" class="text-center text-danger">載入失敗</td></tr>');
        }
    });
}

// ===== 渲染單身表格 (含勾選欄) =====
function renderDetailTable(items) {
    const tbody = document.getElementById('detailTableBody');
    clearDetailSelection();

    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">此單頭無單身明細</td></tr>';
        return;
    }

    tbody.innerHTML = items.map(item => `
        <tr class="cursor-pointer" data-tb003="${item.TB003}">
            <td><input type="checkbox" class="detail-row-check" value="${item.TB003}" /></td>
            <td>${item.TB003}</td>
            <td>${item.TB004}</td>
            <td>${item.TB005 || ''}</td>
            <td class="text-end">${Number(item.TB006).toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</td>
            <td class="text-end">${Number(item.TB007).toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</td>
            <td class="text-end">${Number(item.TB008).toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</td>
            <td>${item.TB009 || ''}</td>
        </tr>
    `).join('');
}

// ===== 清除單身選取 =====
function clearDetailSelection() {
    selectedDetailRows = [];
    $('.detail-row-check').prop('checked', false);
    $('#detailCheckAll').prop('checked', false);
    updateDetailButtonState();
}

// ===== 更新單身按鈕狀態 =====
function updateDetailButtonState() {
    $('#btnEditDetail').prop('disabled', selectedDetailRows.length !== 1);
    $('#btnDeleteDetail').prop('disabled', selectedDetailRows.length === 0);
}

// ===== 開啟單身 Modal =====
function openDetailModal(apiBase, mode, tb003) {
    $('#detailFormMode').val(mode);
    $('#detailForm')[0].reset();
    $('#fd_TB008').val('0');

    if (mode === 'create') {
        $('#detailModalTitle').text('新增明細');
        $('#fd_TB003').prop('readonly', false).val(getNextDetailSeq());
    } else {
        $('#detailModalTitle').text('編輯明細');
        $('#fd_TB003').prop('readonly', true);
        // 載入既有明細
        $.ajax({
            url: `${apiBase}/${currentHeaderTA001}/${currentHeaderTA002}/details/${tb003}`,
            method: 'GET',
            success: function (result) {
                if (result.Success) {
                    const d = result.Data;
                    $('#fd_TB003').val(d.TB003);
                    $('#fd_TB004').val(d.TB004);
                    $('#fd_TB005').val(d.TB005 || '');
                    $('#fd_TB006').val(d.TB006);
                    $('#fd_TB007').val(d.TB007);
                    $('#fd_TB008').val(d.TB008);
                    $('#fd_TB009').val(d.TB009 || '');
                }
            }
        });
    }

    new bootstrap.Modal('#detailModal').show();
}

// ===== 取得下一個序號 =====
function getNextDetailSeq() {
    let maxSeq = 0;
    $('#detailTableBody tr .detail-row-check').each(function () {
        const seq = parseInt($(this).val()) || 0;
        if (seq > maxSeq) maxSeq = seq;
    });
    return String(maxSeq + 1).padStart(4, '0');
}

// ===== 儲存單身 =====
function saveDetailForm(apiBase) {
    const mode = $('#detailFormMode').val();
    const tb003 = $('#fd_TB003').val().trim();

    const payload = {
        TB003: tb003,
        TB004: $('#fd_TB004').val().trim(),
        TB005: $('#fd_TB005').val().trim(),
        TB006: parseFloat($('#fd_TB006').val()) || 0,
        TB007: parseFloat($('#fd_TB007').val()) || 0,
        TB008: parseFloat($('#fd_TB008').val()) || 0,
        TB009: $('#fd_TB009').val().trim()
    };

    let url, method;
    if (mode === 'create') {
        url = `${apiBase}/${currentHeaderTA001}/${currentHeaderTA002}/details`;
        method = 'POST';
    } else {
        url = `${apiBase}/${currentHeaderTA001}/${currentHeaderTA002}/details/${tb003}`;
        method = 'PUT';
    }

    $.ajax({
        url: url,
        method: method,
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (result) {
            if (result.Success) {
                bootstrap.Modal.getInstance('#detailModal').hide();
                showToast(result.Message);
                loadDetails(apiBase, currentHeaderTA001, currentHeaderTA002);
                // 重新整理單頭以更新 DetailCount
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
