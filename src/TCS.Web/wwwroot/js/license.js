const BASE = window.ERP_BASE || '';
const API = BASE + '/api/licenses';
const REQ_API = (lt) => `${BASE}/api/licenses/${encodeURIComponent(lt)}/plants`;
const PLANT_API = BASE + '/api/plants';

const INTEGER_REGEX = /^\d+$/;
const DECIMAL_REGEX = /^\d+(\.\d+)+$/;

let currentPage = 1;
const pageSize = 10;

let cachedLicenses = [];        // 當前頁/篩選結果
let cachedAllCategories = null; // 所有大類，僅初次載入時取
let cachedAllLicensesFull = null; // 全部證照（含小類），T1 推算序號用
let cachedPlants = [];
let selectedLicense = null;        // 完整列物件
let selectedRequirement = null;

let licenseModal, reqModal;

// ---------- 主表載入 / 渲染 ----------
function buildAdvancedQuery() {
    const q = {
        LicenseTypePrefix: $('#adv-LicenseTypePrefix').val().trim(),
        DescriptionContains: $('#adv-DescriptionContains').val().trim(),
        Category: $('#adv-Category').val(),
        HoursMin: $('#adv-HoursMin').val(),
        HoursMax: $('#adv-HoursMax').val(),
        YearsMin: $('#adv-YearsMin').val(),
        YearsMax: $('#adv-YearsMax').val()
    };
    const empty =
        !q.LicenseTypePrefix && !q.DescriptionContains && !q.Category
        && q.HoursMin === '' && q.HoursMax === ''
        && q.YearsMin === '' && q.YearsMax === '';
    return empty ? null : q;
}

async function loadLicenses() {
    const advanced = buildAdvancedQuery();
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (advanced) {
        Object.entries(advanced).forEach(([k, v]) => {
            if (v !== '' && v !== null && v !== undefined) params.set(`query.${k}`, v);
        });
    } else {
        const search = $('#search').val();
        if (search) params.set('search', search);
    }

    const res = await fetch(`${API}?${params}`);
    if (!res.ok) { Toast.error(await readErrorMessage(res, '載入失敗')); return; }
    const data = await res.json();
    cachedLicenses = data.Items || [];
    renderTable(cachedLicenses);
    renderPagination(data.TotalPages, data.CurrentPage);
    clearSelection();
}

function renderTable(items) {
    const $tbody = $('#license-table tbody').empty();
    if (!items.length) {
        $tbody.append($('<tr></tr>').append($('<td colspan="5" class="text-center text-muted"></td>').text('（無資料）')));
        return;
    }
    items.forEach(item => {
        const $tr = $('<tr></tr>').data('row', item);
        $('<td></td>').text(item.LicenseType ?? '').appendTo($tr);
        $('<td></td>').text(item.Description ?? '').appendTo($tr);
        $('<td></td>').text(item.Category ?? '').appendTo($tr);
        $('<td></td>').text(item.Hours ?? '').appendTo($tr);
        $('<td></td>').text(item.Years ?? '').appendTo($tr);
        $tr.on('click', () => selectLicense($tr, item));
        $tbody.append($tr);
    });
}

function renderPagination(totalPages, page) {
    const $ul = $('#pagination').empty();
    if (totalPages <= 1) return;

    const WINDOW = 2; // 當前頁前後各顯示幾頁
    const go = (i) => { if (i !== page) { currentPage = i; loadLicenses(); } };

    const addPage = (i) => {
        const $li = $('<li></li>').addClass('page-item' + (i === page ? ' active' : ''));
        $('<a class="page-link" href="#"></a>')
            .text(i)
            .on('click', (e) => { e.preventDefault(); go(i); })
            .appendTo($li);
        $ul.append($li);
    };
    const addNav = (label, target, disabled) => {
        const $li = $('<li></li>').addClass('page-item' + (disabled ? ' disabled' : ''));
        $('<a class="page-link" href="#"></a>')
            .html(label)
            .on('click', (e) => { e.preventDefault(); if (!disabled) go(target); })
            .appendTo($li);
        $ul.append($li);
    };
    const addEllipsis = () => {
        $ul.append($('<li class="page-item disabled"></li>')
            .append($('<span class="page-link"></span>').text('…')));
    };

    addNav('&laquo;', 1, page === 1);          // 最前頁
    addNav('&lsaquo;', page - 1, page === 1);  // 上一頁

    const start = Math.max(1, page - WINDOW);
    const end = Math.min(totalPages, page + WINDOW);
    if (start > 1) {
        addPage(1);
        if (start > 2) addEllipsis();
    }
    for (let i = start; i <= end; i++) addPage(i);
    if (end < totalPages) {
        if (end < totalPages - 1) addEllipsis();
        addPage(totalPages);
    }

    addNav('&rsaquo;', page + 1, page === totalPages);  // 下一頁
    addNav('&raquo;', totalPages, page === totalPages);  // 最後頁
}

// ---------- 選列 / 高亮 ----------
function selectLicense($tr, item) {
    $('#license-table tbody tr.row-selected').removeClass('row-selected');
    $tr.addClass('row-selected');
    selectedLicense = item;
    TcsAuth.enableIfAllowed('#btn-edit');
    TcsAuth.enableIfAllowed('#btn-delete');
    TcsAuth.enableIfAllowed('#btn-req-add');
    $('#req-license-label').text(`(${item.LicenseType} ${item.Description})`);
    loadRequirements(item.LicenseType);
}

function clearSelection() {
    selectedLicense = null;
    selectedRequirement = null;
    $('#license-table tbody tr.row-selected').removeClass('row-selected');
    $('#btn-edit, #btn-delete').prop('disabled', true);
    $('#btn-req-add, #btn-req-edit, #btn-req-delete').prop('disabled', true);
    $('#req-license-label').text('');
    const $tbody = $('#req-table tbody').empty();
    $tbody.append($('<tr></tr>').append($('<td colspan="3" class="text-center text-muted"></td>').text('請先選擇上方證照')));
}

// ---------- 廠別需求子表 ----------
async function loadRequirements(licenseType) {
    selectedRequirement = null;
    $('#btn-req-edit, #btn-req-delete').prop('disabled', true);
    // 行選列觸發前先重設子表選列按鈕為 disabled，等選了子表列再依 action 啟用

    const res = await fetch(REQ_API(licenseType));
    const $tbody = $('#req-table tbody').empty();
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
    items.forEach(r => {
        const $tr = $('<tr></tr>').data('row', r);
        $('<td></td>').text(r.Plant ?? '').appendTo($tr);
        $('<td></td>').text(r.PlantName ?? '').appendTo($tr);
        $('<td></td>').text(r.RequiredCount ?? '').appendTo($tr);
        $tr.on('click', () => {
            $('#req-table tbody tr.row-selected').removeClass('row-selected');
            $tr.addClass('row-selected');
            selectedRequirement = r;
            TcsAuth.enableIfAllowed('#btn-req-edit');
            TcsAuth.enableIfAllowed('#btn-req-delete');
        });
        $tbody.append($tr);
    });
}

// ---------- License Modal ----------
async function openLicenseModal(mode, item) {
    $('#license-modal-error').addClass('d-none').text('');
    $('#license-modal-title').text(mode === 'create' ? '新增證照' : '修改證照');

    await ensureAllCategoriesLoaded();
    populateCategoryOptions();

    if (mode === 'create') {
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-Category').val('').prop('disabled', false);
        $('#m-Description').val('');
        $('#m-Hours').val('');
        $('#m-Years').val('');
    } else {
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-Description').val(item.Description ?? '');
        const editIsMajor = isLicenseTypeMajor(item.LicenseType);
        $('#m-Category').val(editIsMajor ? '__MAJOR__' : (item.Category ?? '')).prop('disabled', true);
        $('#m-Hours').val(item.Hours ?? '');
        $('#m-Years').val(item.Years ?? '');
    }
    $('#license-form').data('mode', mode);
    syncCategoryVisibility();
    licenseModal.show();
}

function populateCategoryOptions() {
    const $sel = $('#m-Category').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($sel);
    $('<option></option>').val('__MAJOR__').text('大類').appendTo($sel);
    // 隱藏 '99'（其他）：其他大類不會有對應的小類
    (cachedAllCategories || []).filter(x => x.LicenseType !== '99').forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($sel);
    });
}

async function ensureAllCategoriesLoaded() {
    if (cachedAllCategories !== null) return;
    const res = await fetch(`${API}?page=1&pageSize=9999`);
    if (!res.ok) { cachedAllCategories = []; cachedAllLicensesFull = []; return; }
    const data = await res.json();
    cachedAllLicensesFull = data.Items || [];
    cachedAllCategories = cachedAllLicensesFull.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));

    const $sel = $('#adv-Category').empty();
    $('<option></option>').val('').text('（不限）').appendTo($sel);
    cachedAllCategories.forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($sel);
    });
}

function suggestNextLicenseType(selectedCategory) {
    if (!cachedAllLicensesFull) return '';
    if (selectedCategory === '__MAJOR__') {
        const majors = cachedAllLicensesFull
            .filter(x => INTEGER_REGEX.test(x.LicenseType) && x.LicenseType !== '99')
            .map(x => parseInt(x.LicenseType, 10));
        return String(majors.length ? Math.max(...majors) + 1 : 1);
    }
    if (selectedCategory && INTEGER_REGEX.test(selectedCategory)) {
        const subs = cachedAllLicensesFull
            .filter(x => x.Category === selectedCategory)
            .map(x => {
                const parts = x.LicenseType.split('.');
                return parseInt(parts[parts.length - 1], 10);
            })
            .filter(n => !isNaN(n));
        const nextIdx = subs.length ? Math.max(...subs) + 1 : 1;
        return `${selectedCategory}.${nextIdx}`;
    }
    return '';
}

function clearAdvancedFields() {
    $('#advanced-search').find('input, select').val('');
}

function isLicenseTypeMajor(value) {
    return INTEGER_REGEX.test((value ?? '').trim());
}

function isLicenseTypeMinor(value) {
    return DECIMAL_REGEX.test((value ?? '').trim());
}

function syncCategoryVisibility() {
    // Category wrap is always visible — it is the primary input field
    $('#m-Category-wrap').show();
    // Hours and Years are always optional
    $('#m-Hours-required, #m-Years-required').addClass('d-none');
    $('#m-Hours, #m-Years').prop('required', false);
}

async function submitLicense(e) {
    e.preventDefault();
    const lt = $('#m-LicenseType').val().trim();
    const isMajor = isLicenseTypeMajor(lt);
    const isMinor = isLicenseTypeMinor(lt);

    if (!isMajor && !isMinor) {
        showModalError('#license-modal-error', '證照類別格式不正確：純整數（大類）或含小數點（小類）');
        return;
    }

    const catVal = $('#m-Category').val();
    const body = {
        LicenseType: lt,
        Description: $('#m-Description').val().trim(),
        Category: (isMajor || catVal === '__MAJOR__') ? null : (catVal.trim() || null),
        Hours: $('#m-Hours').val() !== '' ? parseInt($('#m-Hours').val(), 10) : null,
        Years: $('#m-Years').val() !== '' ? parseInt($('#m-Years').val(), 10) : null
    };

    // 小類前端基本檢核（後端 FluentValidation 會再次驗證）
    if (isMinor) {
        if (!body.Category) { showModalError('#license-modal-error', '小類需指定對應大類'); return; }
        if (body.Hours != null && body.Hours <= 0) { showModalError('#license-modal-error', '時數若填寫必須 > 0'); return; }
        if (body.Years != null && body.Years < 1) { showModalError('#license-modal-error', '年數若填寫必須 ≥ 1'); return; }
    }

    const mode = $('#license-form').data('mode');
    const url = mode === 'create' ? API : `${API}/${encodeURIComponent(lt)}`;
    const method = mode === 'create' ? 'POST' : 'PUT';
    const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok || res.status === 201) {
        licenseModal.hide();
        cachedAllCategories = null;
        cachedAllLicensesFull = null;
        Toast.success(mode === 'create' ? '證照已新增' : '證照已更新');
        await loadLicenses();
        return;
    }
    const msg = await readErrorMessage(res, '儲存失敗');
    showModalError('#license-modal-error', msg);
}

async function deleteSelectedLicense() {
    if (!selectedLicense) return;
    if (!confirm(`確認刪除 ${selectedLicense.LicenseType}？`)) return;
    const res = await fetch(`${API}/${encodeURIComponent(selectedLicense.LicenseType)}`, { method: 'DELETE' });
    if (res.ok) { Toast.success('證照已刪除'); await loadLicenses(); return; }
    Toast.error(await readErrorMessage(res, '刪除失敗'));
}

// ---------- Plant Requirement Modal ----------
async function ensurePlantsLoaded() {
    if (cachedPlants.length) return;
    const res = await fetch(PLANT_API);
    if (!res.ok) { cachedPlants = []; return; }
    cachedPlants = await res.json();
}

async function openRequirementModal(mode, item) {
    if (!selectedLicense) return;
    $('#req-modal-error').addClass('d-none').text('');
    $('#req-modal-title').text(mode === 'create' ? '新增廠別需求' : '修改廠別需求');
    await ensurePlantsLoaded();

    const $sel = $('#m-Plant').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($sel);
    cachedPlants.forEach(p => {
        const label = (p.PlantName ? `${p.PlantCode} ${p.PlantName}` : p.PlantCode);
        $('<option></option>').val(p.PlantCode).text(label).appendTo($sel);
    });

    if (mode === 'create') {
        $('#m-Plant').val('').prop('disabled', false);
        $('#m-RequiredCount').val('');
    } else {
        $('#m-Plant').val(item.Plant).prop('disabled', true);
        $('#m-RequiredCount').val(item.RequiredCount);
    }
    $('#req-form').data('mode', mode);
    reqModal.show();
}

async function submitRequirement(e) {
    e.preventDefault();
    if (!selectedLicense) return;
    const plant = $('#m-Plant').val();
    const requiredCount = parseInt($('#m-RequiredCount').val(), 10);
    if (!plant) { showModalError('#req-modal-error', '請選擇廠別'); return; }
    if (Number.isNaN(requiredCount) || requiredCount < 0) { showModalError('#req-modal-error', '需求數必須 ≥ 0'); return; }

    const body = { LicenseType: selectedLicense.LicenseType, Plant: plant, RequiredCount: requiredCount };
    const mode = $('#req-form').data('mode');
    const base = REQ_API(selectedLicense.LicenseType);
    const url = mode === 'create' ? base : `${base}/${encodeURIComponent(plant)}`;
    const method = mode === 'create' ? 'POST' : 'PUT';
    const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok || res.status === 201) {
        reqModal.hide();
        Toast.success(mode === 'create' ? '廠別需求已新增' : '廠別需求已更新');
        await loadRequirements(selectedLicense.LicenseType);
        return;
    }
    showModalError('#req-modal-error', await readErrorMessage(res, '儲存失敗'));
}

async function deleteSelectedRequirement() {
    if (!selectedLicense || !selectedRequirement) return;
    if (!confirm(`確認刪除 ${selectedRequirement.Plant}？`)) return;
    const url = `${REQ_API(selectedLicense.LicenseType)}/${encodeURIComponent(selectedRequirement.Plant)}`;
    const res = await fetch(url, { method: 'DELETE' });
    if (res.ok) { Toast.success('廠別需求已刪除'); await loadRequirements(selectedLicense.LicenseType); return; }
    Toast.error(await readErrorMessage(res, '刪除失敗'));
}

// ---------- 共用 ----------
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

// ---------- 初始化 ----------
$(function () {
    licenseModal = new bootstrap.Modal('#license-modal');
    reqModal = new bootstrap.Modal('#req-modal');

    // 依 JWT action 控制按鈕（spec §6-3）
    TcsAuth.applyButtonGuards({
        '#btn-add':        '新增',
        '#btn-edit':       '修改',
        '#btn-delete':     '刪除',
        '#btn-req-add':    '新增',
        '#btn-req-edit':   '修改',
        '#btn-req-delete': '刪除'
    });

    ensureAllCategoriesLoaded();

    $('#btn-search').on('click', () => {
        // 快速搜尋觸發時，進階搜尋自動清空
        clearAdvancedFields();
        currentPage = 1;
        loadLicenses();
    });
    $('#btn-adv-search').on('click', () => {
        // 進階生效時，快速搜尋自動清空（spec §7-1）
        $('#search').val('');
        currentPage = 1;
        loadLicenses();
    });
    $('#btn-adv-reset').on('click', () => {
        clearAdvancedFields();
        currentPage = 1;
        loadLicenses();
    });
    $('#btn-add').on('click', () => openLicenseModal('create'));
    $('#btn-edit').on('click', () => { if (selectedLicense) openLicenseModal('edit', selectedLicense); });
    $('#btn-delete').on('click', deleteSelectedLicense);

    $('#btn-req-add').on('click', () => openRequirementModal('create'));
    $('#btn-req-edit').on('click', () => { if (selectedRequirement) openRequirementModal('edit', selectedRequirement); });
    $('#btn-req-delete').on('click', deleteSelectedRequirement);

    $('#license-form').on('submit', submitLicense);
    $('#req-form').on('submit', submitRequirement);
    $('#m-LicenseType').on('input', syncCategoryVisibility);
    $('#m-Category').on('change', function () {
        const val = $(this).val();
        const suggested = suggestNextLicenseType(val);
        if (suggested) {
            $('#m-LicenseType').val(suggested).prop('disabled', true);
        } else {
            $('#m-LicenseType').val('').prop('disabled', false);
        }
        syncCategoryVisibility();
    });

    loadLicenses();
});
