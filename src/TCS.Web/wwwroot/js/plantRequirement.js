// 廠別需求總覽（todo-2026-07-09 T3）：廠別視角反查證照需求，本頁為廠別需求唯一維護入口。
// 讀取走 GET /api/plants/{plant}/requirements；寫入沿用既有 api/licenses/{licenseType}/plants。
const BASE = window.ERP_BASE || '';
const PLANT_API = BASE + '/api/plants';
const LICENSE_API = BASE + '/api/licenses';
const OVERVIEW_API = (plant) => `${BASE}/api/plants/${encodeURIComponent(plant)}/requirements`;
const REQ_API = (lt) => `${BASE}/api/licenses/${encodeURIComponent(lt)}/plants`;

let cachedPlants = [];
let cachedAllLicenses = null;   // 全部證照（沿用現行語意：大類/小類皆可掛需求）
let currentPlant = '';
let selectedRequirement = null;

let reqModal;

// ---------- 廠別下拉 ----------
async function loadPlants() {
    const res = await fetch(PLANT_API);
    if (!res.ok) { Toast.error('廠別載入失敗'); return; }
    cachedPlants = await res.json();
    const $sel = $('#plant-select').empty();
    $('<option></option>').val('').text('-- 請選擇廠別 --').appendTo($sel);
    cachedPlants.forEach(p => {
        const label = p.PlantName ? `${p.PlantCode} ${p.PlantName}` : p.PlantCode;
        $('<option></option>').val(p.PlantCode).text(label).appendTo($sel);
    });
}

// ---------- 需求列表 ----------
async function loadRequirements() {
    selectedRequirement = null;
    $('#btn-edit, #btn-delete').prop('disabled', true);

    const $tbody = $('#req-table tbody').empty();
    if (!currentPlant) {
        $('#req-plant-label').text('');
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-muted"></td>').text('請先選擇廠別')));
        return;
    }
    const plant = cachedPlants.find(p => p.PlantCode === currentPlant);
    $('#req-plant-label').text(plant && plant.PlantName ? `(${plant.PlantCode} ${plant.PlantName})` : `(${currentPlant})`);

    const res = await fetch(OVERVIEW_API(currentPlant));
    if (!res.ok) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-danger"></td>').text('載入失敗')));
        return;
    }
    const items = await res.json();
    if (!items.length) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-muted"></td>').text('（此廠別尚無證照需求）')));
        return;
    }
    items.forEach(r => {
        const $tr = $('<tr></tr>').data('row', r);
        $('<td></td>').text(r.LicenseType ?? '').appendTo($tr);
        $('<td></td>').text(r.Description ?? '').appendTo($tr);
        $('<td></td>').text(r.RequiredCount ?? '').appendTo($tr);
        $tr.on('click', () => {
            $('#req-table tbody tr.row-selected').removeClass('row-selected');
            $tr.addClass('row-selected');
            selectedRequirement = r;
            TcsAuth.enableIfAllowed('#btn-edit');
            TcsAuth.enableIfAllowed('#btn-delete');
        });
        $tbody.append($tr);
    });
}

// ---------- Modal ----------
async function ensureAllLicensesLoaded() {
    if (cachedAllLicenses !== null) return;
    const res = await fetch(`${LICENSE_API}?page=1&pageSize=9999`);
    if (!res.ok) { cachedAllLicenses = []; return; }
    const data = await res.json();
    cachedAllLicenses = data.Items || [];
}

async function openRequirementModal(mode, item) {
    if (!currentPlant) return;
    $('#req-modal-error').addClass('d-none').text('');
    $('#req-modal-title').text(mode === 'create' ? '新增證照需求' : '修改證照需求');
    await ensureAllLicensesLoaded();

    const $sel = $('#m-LicenseType').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($sel);
    cachedAllLicenses.forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($sel);
    });

    if (mode === 'create') {
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-RequiredCount').val('');
    } else {
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-RequiredCount').val(item.RequiredCount);
    }
    $('#req-form').data('mode', mode);
    reqModal.show();
}

async function submitRequirement(e) {
    e.preventDefault();
    if (!currentPlant) return;
    const licenseType = $('#m-LicenseType').val();
    const requiredCount = parseInt($('#m-RequiredCount').val(), 10);
    if (!licenseType) { showModalError('#req-modal-error', '請選擇證照'); return; }
    if (Number.isNaN(requiredCount) || requiredCount < 1) { showModalError('#req-modal-error', '需求數必須 ≥ 1'); return; }

    const body = { LicenseType: licenseType, Plant: currentPlant, RequiredCount: requiredCount };
    const mode = $('#req-form').data('mode');
    const base = REQ_API(licenseType);
    const url = mode === 'create' ? base : `${base}/${encodeURIComponent(currentPlant)}`;
    const method = mode === 'create' ? 'POST' : 'PUT';
    const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok || res.status === 201) {
        reqModal.hide();
        Toast.success(mode === 'create' ? '證照需求已新增' : '證照需求已更新');
        await loadRequirements();
        return;
    }
    showModalError('#req-modal-error', await readErrorMessage(res, '儲存失敗'));
}

async function deleteSelectedRequirement() {
    if (!currentPlant || !selectedRequirement) return;
    if (!confirm(`確認刪除 ${selectedRequirement.LicenseType} ${selectedRequirement.Description ?? ''} 的需求？`)) return;
    const url = `${REQ_API(selectedRequirement.LicenseType)}/${encodeURIComponent(currentPlant)}`;
    const res = await fetch(url, { method: 'DELETE' });
    if (res.ok) { Toast.success('證照需求已刪除'); await loadRequirements(); return; }
    Toast.error(await readErrorMessage(res, '刪除失敗'));
}

// ---------- Excel 匯出 ----------
async function exportExcel() {
    if (!currentPlant) return;
    const res = await fetch(`${BASE}/api/export/plant-requirements?plant=${encodeURIComponent(currentPlant)}`);
    if (!res.ok) { Toast.error(await readErrorMessage(res, '匯出失敗')); return; }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    a.download = `plant_requirements_${currentPlant.trim()}_${today}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
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
    reqModal = new bootstrap.Modal('#req-modal');

    // 依 JWT action 控制按鈕（spec §6-3）
    TcsAuth.applyButtonGuards({
        '#btn-add':    '新增',
        '#btn-edit':   '修改',
        '#btn-delete': '刪除',
        '#btn-export': '列印'
    });

    $('#plant-select').on('change', function () {
        currentPlant = $(this).val();
        if (currentPlant) {
            TcsAuth.enableIfAllowed('#btn-add');
            TcsAuth.enableIfAllowed('#btn-export');
        } else {
            $('#btn-add, #btn-export').prop('disabled', true);
        }
        loadRequirements();
    });

    $('#btn-add').on('click', () => openRequirementModal('create'));
    $('#btn-edit').on('click', () => { if (selectedRequirement) openRequirementModal('edit', selectedRequirement); });
    $('#btn-delete').on('click', deleteSelectedRequirement);
    $('#btn-export').on('click', exportExcel);
    $('#req-form').on('submit', submitRequirement);

    loadPlants();
});
