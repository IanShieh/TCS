const BASE = window.ERP_BASE || '';
const HEADER_API = BASE + '/api/training-headers';
const DETAIL_API = BASE + '/api/training-details';
const EMPLOYEE_API = BASE + '/api/employees';
const LICENSE_API = BASE + '/api/licenses';
const LICENSE_PLANT_API = (lt) => `${BASE}/api/licenses/${encodeURIComponent(lt)}/plants`;

const INTEGER_REGEX = /^\d+$/;

let currentPage = 1;
const pageSize = 20;

let cachedEmployees = null;     // EmployeeDto[]
let employeeMap = {};            // EmployeeId → EmployeeDto
let cachedAllLicenses = null;    // all LicenseMasterDto[]
let cachedMinorLicenses = null;  // derived: 小類 only
let licenseMap = {};             // LicenseType → LicenseMasterDto

let selectedHeader = null;
let selectedDetail = null;

let headerModal, detailModal;

// ---------- 主表載入 / 渲染 ----------
function buildAdvancedQuery() {
    const q = {
        EmployeeId: $('#adv-EmployeeId').val().trim(),
        NameContains: $('#adv-NameContains').val().trim(),
        Department: $('#adv-Department').val(),
        LicenseType: $('#adv-LicenseType').val(),
        ExpiredOnly: $('#adv-ExpiredOnly').is(':checked'),
        UnmetHoursOnly: $('#adv-UnmetHoursOnly').is(':checked'),
        NextReviewFrom: $('#adv-NextReviewFrom').val(),
        NextReviewTo: $('#adv-NextReviewTo').val()
    };
    const empty =
        !q.EmployeeId && !q.NameContains && !q.Department && !q.LicenseType
        && !q.ExpiredOnly && !q.UnmetHoursOnly
        && !q.NextReviewFrom && !q.NextReviewTo;
    return empty ? null : q;
}

async function loadHeaders() {
    const advanced = buildAdvancedQuery();
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (advanced) {
        Object.entries(advanced).forEach(([k, v]) => {
            if (v === '' || v === null || v === undefined || v === false) return;
            params.set(`query.${k}`, v);
        });
    } else {
        const search = $('#search').val();
        if (search) params.set('query.Search', search);
    }
    const res = await fetch(`${HEADER_API}?${params}`);
    if (!res.ok) { Toast.error(await readErrorMessage(res, '載入失敗')); return; }
    const data = await res.json();
    renderTable(data.Items || []);
    renderPagination(data.TotalPages, data.CurrentPage);
    clearHeaderSelection();
}

function renderTable(items) {
    const $tbody = $('#training-table tbody').empty();
    if (!items.length) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="12" class="text-center text-muted"></td>').text('（無資料）')
        ));
        return;
    }
    items.forEach(r => {
        const $tr = $('<tr></tr>').data('row', r);
        $('<td></td>').text(r.EmployeeId ?? '').appendTo($tr);
        $('<td></td>').text(r.EmployeeName ?? '').appendTo($tr);
        $('<td></td>').text(r.Department ?? '').appendTo($tr);
        $('<td></td>').text(formatHireDate(r.HireDate)).appendTo($tr);
        $('<td></td>').text(r.LicenseType ?? '').appendTo($tr);
        $('<td></td>').text(r.Description ?? '').appendTo($tr);
        $('<td></td>').text(r.RequiredHours ?? '').appendTo($tr);
        $('<td></td>').text(r.LatestRetrainDate ?? '—').appendTo($tr);
        $('<td></td>').text(r.RemainingHours ?? '').appendTo($tr);
        $('<td></td>').text(r.NextReviewDate ?? '—').appendTo($tr);
        $('<td></td>').text(r.Plant ?? '').appendTo($tr);
        $('<td></td>').text(r.Remark ?? '').appendTo($tr);
        $tr.on('click', () => selectHeader($tr, r));
        $tbody.append($tr);
    });
}

function renderPagination(totalPages, page) {
    const $ul = $('#pagination').empty();
    for (let i = 1; i <= totalPages; i++) {
        const $li = $('<li></li>').addClass('page-item' + (i === page ? ' active' : ''));
        $('<a class="page-link" href="#"></a>')
            .text(i)
            .on('click', (e) => { e.preventDefault(); currentPage = i; loadHeaders(); })
            .appendTo($li);
        $ul.append($li);
    }
}

// ---------- 選列 ----------
function selectHeader($tr, item) {
    $('#training-table tbody tr.row-selected').removeClass('row-selected');
    $tr.addClass('row-selected');
    selectedHeader = item;
    TcsAuth.enableIfAllowed('#btn-edit');
    TcsAuth.enableIfAllowed('#btn-delete');
    TcsAuth.enableIfAllowed('#btn-detail-add');
    $('#detail-header-label').text(`(${item.EmployeeId} ${item.EmployeeName ?? ''} / ${item.LicenseType})`);
    loadDetails(item.EmployeeId, item.LicenseType);
}

function clearHeaderSelection() {
    selectedHeader = null;
    selectedDetail = null;
    $('#training-table tbody tr.row-selected').removeClass('row-selected');
    $('#btn-edit, #btn-delete').prop('disabled', true);
    $('#btn-detail-add, #btn-detail-edit, #btn-detail-delete').prop('disabled', true);
    $('#detail-header-label').text('');
    const $tbody = $('#detail-table tbody').empty();
    $tbody.append($('<tr></tr>').append(
        $('<td colspan="3" class="text-center text-muted"></td>').text('請先選擇上方受訓單頭')
    ));
}

// ---------- 單身子表 ----------
async function loadDetails(employeeId, licenseType) {
    selectedDetail = null;
    $('#btn-detail-edit, #btn-detail-delete').prop('disabled', true);

    const url = `${DETAIL_API}/${encodeURIComponent(employeeId)}/${encodeURIComponent(licenseType)}`;
    const res = await fetch(url);
    const $tbody = $('#detail-table tbody').empty();
    if (!res.ok) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-danger"></td>').text('載入失敗')
        ));
        return;
    }
    const items = await res.json();
    if (!items.length) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-muted"></td>').text('（無資料）')
        ));
        return;
    }
    items.forEach(d => {
        const $tr = $('<tr></tr>').data('row', d);
        $('<td></td>').text(d.TrainingDate ?? '').appendTo($tr);
        $('<td></td>').text(trainingTypeLabel(d.TrainingType)).appendTo($tr);
        $('<td></td>').text(d.Hours ?? '').appendTo($tr);
        $tr.on('click', () => {
            $('#detail-table tbody tr.row-selected').removeClass('row-selected');
            $tr.addClass('row-selected');
            selectedDetail = d;
            TcsAuth.enableIfAllowed('#btn-detail-edit');
            TcsAuth.enableIfAllowed('#btn-detail-delete');
        });
        $tbody.append($tr);
    });
}

function trainingTypeLabel(t) {
    return t === 1 ? '取得證照' : t === 2 ? '回訓' : '';
}

// ---------- 員工 / 證照快取 ----------
async function ensureEmployeesLoaded() {
    if (cachedEmployees) return;
    const res = await fetch(EMPLOYEE_API);
    if (!res.ok) { cachedEmployees = []; return; }
    cachedEmployees = await res.json();
    employeeMap = {};
    cachedEmployees.forEach(e => { employeeMap[e.EmployeeId] = e; });

    // datalist 一次性塞入
    const $list = $('#emp-list').empty();
    cachedEmployees.forEach(e => {
        const label = e.Name ? `${e.EmployeeId} - ${e.Name}` : e.EmployeeId;
        $('<option></option>').val(e.EmployeeId).text(label).appendTo($list);
    });
}

async function ensureAllLicensesLoaded() {
    if (cachedAllLicenses) return;
    const res = await fetch(`${LICENSE_API}?page=1&pageSize=9999`);
    if (!res.ok) { cachedAllLicenses = []; cachedMinorLicenses = []; return; }
    const data = await res.json();
    const all = data.Items || [];
    cachedAllLicenses = all;
    licenseMap = {};
    all.forEach(x => { licenseMap[x.LicenseType] = x; });
    cachedMinorLicenses = cachedAllLicenses.filter(x => !x.IsCategory && !INTEGER_REGEX.test(x.LicenseType));
}

async function loadPlantOptions(licenseType) {
    const $sel = $('#m-Plant').empty();
    $('<option></option>').val('').text('（不選廠別）').appendTo($sel);
    if (!licenseType) return;
    const res = await fetch(LICENSE_PLANT_API(licenseType));
    if (!res.ok) return;
    const items = await res.json();
    items.forEach(p => {
        const label = p.PlantName ? `${p.Plant} ${p.PlantName}` : p.Plant;
        $('<option></option>').val(p.Plant).text(label).appendTo($sel);
    });
}

// ---------- Header Modal ----------
async function openHeaderModal(mode, item) {
    $('#header-modal-error').addClass('d-none').text('');
    $('#header-modal-title').text(mode === 'create' ? '新增受訓單頭' : '修改受訓單頭');

    await Promise.all([ensureEmployeesLoaded(), ensureAllLicensesLoaded()]);

    const $licSel = $('#m-LicenseType').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($licSel);
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`).appendTo($grp);
        cachedAllLicenses.filter(x => x.Category === cat.LicenseType).forEach(x => {
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp);
        });
        $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`).attr('data-is-other', 'true').appendTo($grp);
        $grp.appendTo($licSel);
    });

    if (mode === 'create') {
        $('#m-EmployeeId').val('').prop('readonly', false);
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-RequiredHours').val('');
        $('#m-Remark').val('');
        await loadPlantOptions('');
        updateEmployeeHint();
    } else {
        $('#m-EmployeeId').val(item.EmployeeId).prop('readonly', true);
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-RequiredHours').val(item.RequiredHours ?? '');
        $('#m-Remark').val(item.Remark ?? '');
        await loadPlantOptions(item.LicenseType);
        $('#m-Plant').val(item.Plant ?? '');
        updateEmployeeHint();
    }
    $('#header-form').data('mode', mode);
    updateCustomNameVisibility();
    headerModal.show();
}

function updateEmployeeHint() {
    const id = $('#m-EmployeeId').val();
    const e = employeeMap[id];
    $('#m-EmployeeId-hint').text(e ? `${e.Name ?? ''} ${e.Department ? '/ ' + e.Department : ''}` : ' ');
}

function updateRequiredHoursOnLicenseChange() {
    const lt = $('#m-LicenseType').val();
    const lic = licenseMap[lt];
    $('#m-RequiredHours').val(lic && lic.Hours != null ? lic.Hours : '');
}

function updateCustomNameVisibility() {
    const isOther = $('#m-LicenseType option:selected').data('isOther') === true;
    if (isOther) {
        $('#m-CustomName-group').removeClass('d-none');
        $('#m-Remark-group').addClass('d-none');
        $('#m-CustomName').prop('required', true);
    } else {
        $('#m-CustomName-group').addClass('d-none');
        $('#m-Remark-group').removeClass('d-none');
        $('#m-CustomName').prop('required', false);
    }
}

async function submitHeader(e) {
    e.preventDefault();
    const mode = $('#header-form').data('mode');
    const employeeId = $('#m-EmployeeId').val().trim();
    const licenseType = $('#m-LicenseType').val();
    let remark = $('#m-Remark').val().trim() || null;

    if (mode === 'create') {
        if (!employeeMap[employeeId]) {
            showModalError('#header-modal-error', '員工編號不存在於員工主檔');
            return;
        }
        if (!licenseType) {
            showModalError('#header-modal-error', '請選擇證照類別');
            return;
        }
        if ($('#m-LicenseType option:selected').data('isOther') === true) {
            const customName = $('#m-CustomName').val().trim();
            if (!customName) {
                showModalError('#header-modal-error', '請輸入自定義證照名稱');
                return;
            }
            remark = customName;
        }
    }

    const body = { EmployeeId: employeeId, LicenseType: licenseType, Remark: remark, Plant: $('#m-Plant').val() || null };
    const url = mode === 'create'
        ? HEADER_API
        : `${HEADER_API}/${encodeURIComponent(employeeId)}/${encodeURIComponent(licenseType)}`;
    const method = mode === 'create' ? 'POST' : 'PUT';
    const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok || res.status === 201) {
        headerModal.hide();
        Toast.success(mode === 'create' ? '受訓單頭已新增' : '受訓單頭已更新');
        await loadHeaders();
        return;
    }
    showModalError('#header-modal-error', await readErrorMessage(res, '儲存失敗'));
}

async function deleteSelectedHeader() {
    if (!selectedHeader) return;
    const { EmployeeId, LicenseType } = selectedHeader;
    if (!confirm(`確認刪除 ${EmployeeId}/${LicenseType} 的受訓紀錄？`)) return;
    const url = `${HEADER_API}/${encodeURIComponent(EmployeeId)}/${encodeURIComponent(LicenseType)}`;
    const res = await fetch(url, { method: 'DELETE' });
    if (res.ok) { Toast.success('受訓單頭已刪除'); await loadHeaders(); return; }
    Toast.error(await readErrorMessage(res, '刪除失敗'));
}

// ---------- Detail Modal ----------
function openDetailModal(mode, item) {
    if (!selectedHeader) return;
    $('#detail-modal-error').addClass('d-none').text('');
    $('#detail-modal-title').text(mode === 'create' ? '新增受訓紀錄' : '修改受訓紀錄');

    if (mode === 'create') {
        $('#m-TrainingDate').val('').prop('readonly', false);
        $('input[name="m-TrainingType"][value="1"]').prop('checked', true);
        $('#m-Hours').val('');
    } else {
        $('#m-TrainingDate').val(item.TrainingDate).prop('readonly', true);
        $(`input[name="m-TrainingType"][value="${item.TrainingType}"]`).prop('checked', true);
        $('#m-Hours').val(item.Hours);
    }
    $('#detail-form').data('mode', mode);
    if (mode === 'edit') $('#detail-form').data('originalDate', item.TrainingDate);
    detailModal.show();
}

async function submitDetail(e) {
    e.preventDefault();
    if (!selectedHeader) return;
    const mode = $('#detail-form').data('mode');
    const trainingDate = $('#m-TrainingDate').val();
    const trainingType = parseInt($('input[name="m-TrainingType"]:checked').val(), 10);
    const hours = parseFloat($('#m-Hours').val());

    if (!trainingDate) { showModalError('#detail-modal-error', '請選擇受訓日期'); return; }
    if (Number.isNaN(hours) || hours <= 0) { showModalError('#detail-modal-error', '時數必須 > 0'); return; }

    // 不可未來日（前端先擋，後端 validator 也會擋）
    if (trainingDate > todayLocalIso()) {
        showModalError('#detail-modal-error', '受訓日期不可為未來日');
        return;
    }

    const body = {
        EmployeeId: selectedHeader.EmployeeId,
        LicenseType: selectedHeader.LicenseType,
        TrainingDate: trainingDate,
        TrainingType: trainingType,
        Hours: hours
    };

    let url, method;
    if (mode === 'create') {
        url = DETAIL_API;
        method = 'POST';
    } else {
        const dateForRoute = $('#detail-form').data('originalDate');
        url = `${DETAIL_API}/${encodeURIComponent(selectedHeader.EmployeeId)}/${encodeURIComponent(selectedHeader.LicenseType)}/${encodeURIComponent(dateForRoute)}`;
        method = 'PUT';
    }
    const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok || res.status === 201) {
        detailModal.hide();
        Toast.success(mode === 'create' ? '受訓紀錄已新增' : '受訓紀錄已更新');
        await loadDetails(selectedHeader.EmployeeId, selectedHeader.LicenseType);
        // 單身改變後單頭衍生欄位（累計、未達時數）也會變，重新撈當前頁
        await loadHeaders();
        return;
    }
    showModalError('#detail-modal-error', await readErrorMessage(res, '儲存失敗'));
}

async function deleteSelectedDetail() {
    if (!selectedHeader || !selectedDetail) return;
    if (!confirm(`確認刪除 ${selectedDetail.TrainingDate} 的受訓紀錄？`)) return;
    const url = `${DETAIL_API}/${encodeURIComponent(selectedHeader.EmployeeId)}/${encodeURIComponent(selectedHeader.LicenseType)}/${encodeURIComponent(selectedDetail.TrainingDate)}`;
    const res = await fetch(url, { method: 'DELETE' });
    if (res.ok) {
        Toast.success('受訓紀錄已刪除');
        await loadDetails(selectedHeader.EmployeeId, selectedHeader.LicenseType);
        await loadHeaders();
        return;
    }
    Toast.error(await readErrorMessage(res, '刪除失敗'));
}

// ---------- Excel 匯出 ----------
async function exportExcel() {
    const advanced = buildAdvancedQuery();
    const params = new URLSearchParams();
    if (advanced) {
        Object.entries(advanced).forEach(([k, v]) => {
            if (v === '' || v === null || v === undefined || v === false) return;
            params.set(`query.${k}`, v);
        });
    } else {
        const search = $('#search').val();
        if (search) params.set('query.Search', search);
    }
    const res = await fetch(`${BASE}/api/export/training-headers?${params}`);
    if (!res.ok) { Toast.error(await readErrorMessage(res, '匯出失敗')); return; }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    a.download = `training_export_${today}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// ---------- 共用 ----------
function formatHireDate(s) {
    if (!s) return '';
    if (s.length === 8 && /^\d{8}$/.test(s)) {
        return `${s.slice(0,4)}-${s.slice(4,6)}-${s.slice(6,8)}`;
    }
    return s;
}

function todayLocalIso() {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

async function readErrorMessage(res, fallback) {
    try {
        const err = await res.json();
        if (err && err.message) return err.message;
    } catch { /* ignore */ }
    return fallback;
}

function showModalError(selector, msg) {
    $(selector).text(msg || '操作失敗').removeClass('d-none');
}

async function populateAdvancedDropdowns() {
    await Promise.all([ensureEmployeesLoaded(), ensureAllLicensesLoaded()]);

    // 部門：自員工資料去重後排序
    const depts = [...new Set((cachedEmployees || []).map(e => e.Department).filter(Boolean))].sort();
    const $dept = $('#adv-Department').empty();
    $('<option></option>').val('').text('（不限）').appendTo($dept);
    depts.forEach(d => $('<option></option>').val(d).text(d).appendTo($dept));

    // 證照（僅小類）
    const $lic = $('#adv-LicenseType').empty();
    $('<option></option>').val('').text('（不限）').appendTo($lic);
    (cachedMinorLicenses || []).forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($lic);
    });
}

function clearAdvancedFields() {
    $('#advanced-search').find('input[type="text"], input[type="date"], select').val('');
    $('#advanced-search').find('input[type="checkbox"]').prop('checked', false);
}

// ---------- 初始化 ----------
$(function () {
    headerModal = new bootstrap.Modal('#header-modal');
    detailModal = new bootstrap.Modal('#detail-modal');

    // 依 JWT action 控制按鈕（spec §6-3）
    TcsAuth.applyButtonGuards({
        '#btn-add':           '新增',
        '#btn-edit':          '修改',
        '#btn-delete':        '刪除',
        '#btn-export':        '列印',
        '#btn-detail-add':    '新增',
        '#btn-detail-edit':   '修改',
        '#btn-detail-delete': '刪除'
    });

    populateAdvancedDropdowns();

    $('#btn-search').on('click', () => {
        clearAdvancedFields();
        currentPage = 1;
        loadHeaders();
    });
    $('#btn-adv-search').on('click', () => {
        $('#search').val('');
        currentPage = 1;
        loadHeaders();
    });
    $('#btn-adv-reset').on('click', () => {
        clearAdvancedFields();
        currentPage = 1;
        loadHeaders();
    });
    $('#btn-add').on('click', () => openHeaderModal('create'));
    $('#btn-edit').on('click', () => { if (selectedHeader) openHeaderModal('edit', selectedHeader); });
    $('#btn-delete').on('click', deleteSelectedHeader);
    $('#btn-export').on('click', exportExcel);

    $('#btn-detail-add').on('click', () => openDetailModal('create'));
    $('#btn-detail-edit').on('click', () => { if (selectedDetail) openDetailModal('edit', selectedDetail); });
    $('#btn-detail-delete').on('click', deleteSelectedDetail);

    $('#header-form').on('submit', submitHeader);
    $('#detail-form').on('submit', submitDetail);
    $('#m-EmployeeId').on('input change', updateEmployeeHint);
    $('#m-LicenseType').on('change', async function () {
        updateCustomNameVisibility();
        updateRequiredHoursOnLicenseChange();
        await loadPlantOptions($(this).val());
    });

    loadHeaders();
});
