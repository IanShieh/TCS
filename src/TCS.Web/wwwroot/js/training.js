const BASE = window.ERP_BASE || '';
const HEADER_API = BASE + '/api/training-headers';
const DETAIL_API = BASE + '/api/training-details';
const EMPLOYEE_API = BASE + '/api/employees';
const LICENSE_API = BASE + '/api/licenses';
const LICENSE_PLANT_API = (lt) => `${BASE}/api/licenses/${encodeURIComponent(lt)}/plants`;
const PLANT_API = BASE + '/api/plants';

const INTEGER_REGEX = /^\d+$/;

let currentPage = 1;
const pageSize = 10;

let sortBy = 'LicenseType';   // 排序主鍵（EmployeeId / LicenseType），預設證照；另一欄為次要排序
let sortDesc = false;

let selectedEmployee = null;
let cachedAllLicenses = null;    // all LicenseMasterDto[]
let cachedMinorLicenses = null;  // derived: 小類 only
let licenseMap = {};             // LicenseType → LicenseMasterDto

let selectedHeader = null;
let selectedDetail = null;
let currentDetailCount = 0;   // 當前選取表頭的單身筆數（決定新增時類型）
let latestDetailDate = null;  // 當前選取表頭最後一筆受訓日期（append-only 用）

let headerModal, detailModal;

// ---------- 主表載入 / 渲染 ----------
function buildAdvancedQuery() {
    const q = {
        EmployeeId: $('#adv-EmployeeId').val().trim(),
        NameContains: $('#adv-NameContains').val().trim(),
        Department: $('#adv-Department').val(),
        LicenseType: $('#adv-LicenseType').val(),
        Plant: $('#adv-Plant').val(),
        ExpiredOnly: $('#adv-ExpiredOnly').is(':checked'),
        UnmetHoursOnly: $('#adv-UnmetHoursOnly').is(':checked'),
        NextReviewFrom: $('#adv-NextReviewFrom').val(),
        NextReviewTo: $('#adv-NextReviewTo').val()
    };
    const empty =
        !q.EmployeeId && !q.NameContains && !q.Department && !q.LicenseType
        && !q.Plant && !q.ExpiredOnly && !q.UnmetHoursOnly
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
    if (sortBy) {
        params.set('query.SortBy', sortBy);
        if (sortDesc) params.set('query.SortDesc', 'true');
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
            $('<td colspan="13" class="text-center text-muted"></td>').text('（無資料）')
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
        $('<td></td>').text(r.Hours ?? '').appendTo($tr);
        $('<td></td>').text(r.LatestRetrainDate ?? '—').appendTo($tr);
        $('<td></td>').text(r.RemainingHours ?? '').appendTo($tr);
        $('<td></td>').text(r.NextReviewDate ?? '—').appendTo($tr);
        $('<td></td>').text(r.Plant ?? '').appendTo($tr);
        $('<td></td>').text(r.Remark ?? '').appendTo($tr);
        // OverallStatus 數字→標籤（0=回訓完成 1=待回訓 2=已過期 3=無→空白）
        const statusLabel = { 0: '回訓完成', 1: '待回訓', 2: '已過期' }[r.OverallStatus] ?? '';
        $('<td></td>').text(statusLabel)
            .toggleClass('text-danger fw-bold', r.OverallStatus === 2)
            .appendTo($tr);
        $tr.on('click', () => selectHeader($tr, r));
        $tbody.append($tr);
    });
}

function renderPagination(totalPages, page) {
    const $ul = $('#pagination').empty();
    if (totalPages <= 1) return;

    const WINDOW = 2; // 當前頁前後各顯示幾頁
    const go = (i) => { if (i !== page) { currentPage = i; loadHeaders(); } };

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

// ---------- 選列 ----------
function applyHeaderSelection($tr, item, selectDetailDate) {
    $('#training-table tbody tr.row-selected').removeClass('row-selected');
    $tr.addClass('row-selected');
    selectedHeader = item;
    TcsAuth.enableIfAllowed('#btn-edit');
    TcsAuth.enableIfAllowed('#btn-delete');
    TcsAuth.enableIfAllowed('#btn-detail-add');
    $('#detail-header-label').text(`(${item.EmployeeId} ${item.EmployeeName ?? ''} / ${item.LicenseType})`);
    loadDetails(item.EmployeeId, item.LicenseType, selectDetailDate);
}

function selectHeader($tr, item) {
    applyHeaderSelection($tr, item);
}

// 依鍵值在當前頁找到表頭列並選取（新增/修改後回選用）
function selectHeaderByKey(employeeId, licenseType, selectDetailDate) {
    let $match = null;
    $('#training-table tbody tr').each(function () {
        const row = $(this).data('row');
        if (row && row.EmployeeId === employeeId && row.LicenseType === licenseType) {
            $match = $(this);
            return false;
        }
    });
    if ($match) applyHeaderSelection($match, $match.data('row'), selectDetailDate);
}

function clearHeaderSelection() {
    selectedHeader = null;
    selectedDetail = null;
    currentDetailCount = 0;
    latestDetailDate = null;
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
function selectDetailRow($tr, d) {
    $('#detail-table tbody tr.row-selected').removeClass('row-selected');
    $tr.addClass('row-selected');
    selectedDetail = d;
    // append-only：僅最後一筆（最新日期）可編輯／刪除，其餘列鎖定
    if (d.TrainingDate === latestDetailDate) {
        TcsAuth.enableIfAllowed('#btn-detail-edit');
        TcsAuth.enableIfAllowed('#btn-detail-delete');
    } else {
        $('#btn-detail-edit, #btn-detail-delete').prop('disabled', true);
    }
}

async function loadDetails(employeeId, licenseType, selectDate) {
    selectedDetail = null;
    $('#btn-detail-edit, #btn-detail-delete').prop('disabled', true);

    const url = `${DETAIL_API}/${encodeURIComponent(employeeId)}/${encodeURIComponent(licenseType)}`;
    const res = await fetch(url);
    const $tbody = $('#detail-table tbody').empty();
    if (!res.ok) {
        currentDetailCount = 0;
        latestDetailDate = null;
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-danger"></td>').text('載入失敗')
        ));
        return;
    }
    const items = await res.json();
    currentDetailCount = items.length;
    latestDetailDate = items.length
        ? items.reduce((mx, d) => (d.TrainingDate > mx ? d.TrainingDate : mx), items[0].TrainingDate)
        : null;
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
        $tr.on('click', () => selectDetailRow($tr, d));
        if (selectDate && d.TrainingDate === selectDate) selectDetailRow($tr, d);
        $tbody.append($tr);
    });
}

function trainingTypeLabel(t) {
    return t === 1 ? '取得證照' : t === 2 ? '回訓' : '';
}

// ---------- 員工搜尋 ----------
async function searchEmployees(q) {
    const resp = await fetch(`${EMPLOYEE_API}/search?q=${encodeURIComponent(q)}`);
    return resp.ok ? await resp.json() : [];
}

function renderEmployeePicker(employees) {
    const $tbody = $('#emp-picker-tbody').empty();
    employees.forEach(e => {
        $('<tr style="cursor:pointer"></tr>')
            .append($('<td></td>').text(e.EmployeeId))
            .append($('<td></td>').text(e.Name || ''))
            .append($('<td></td>').text(e.Department || ''))
            .append($('<td></td>').text(e.HireDate || ''))
            .on('click', function () {
                selectedEmployee = e;
                $('#m-EmployeeId').val(e.EmployeeId);
                $('#m-EmployeeId-display').val(
                    e.Name ? `${e.EmployeeId} - ${e.Name}` : e.EmployeeId);
                updateEmployeeHint();
                bootstrap.Modal.getInstance(
                    document.getElementById('employee-picker-modal')).hide();
            })
            .appendTo($tbody);
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

    await ensureAllLicensesLoaded();

    const $licSel = $('#m-LicenseType').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($licSel);
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        const children = cachedAllLicenses.filter(x => x.Category === cat.LicenseType);

        if (cat.LicenseType === '99') {
            // 全域其他:大類本身即入口，選取需填自定義名稱
            $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`)
                .attr('data-is-other', 'true').appendTo($grp);
        } else if (children.length === 0) {
            // 無小類 → 獨立真證照:可直接選，IsOther=false，名稱/時數由主檔帶出
            $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`).appendTo($grp);
        } else {
            // 有小類 → 分類抬頭:不放大類本身，列小類 + 其他 sentinel
            children.forEach(x =>
                $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp));
            $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`)
                .attr('data-is-other', 'true').appendTo($grp);
        }
        $grp.appendTo($licSel);
    });

    if (mode === 'create') {
        selectedEmployee = null;
        $('#m-EmployeeId').val('');
        $('#m-EmployeeId-display').val('').prop('disabled', false);
        $('#btn-pick-employee').prop('disabled', false);
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-header-Hours').val('');
        $('#m-header-Years').val('');
        $('#m-Remark').val('');
        $('#m-CustomName').val('');
        await loadPlantOptions('');
        updateEmployeeHint();
    } else {
        $('#m-EmployeeId').val(item.EmployeeId);
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-header-Hours').val(item.Hours ?? '');
        $('#m-header-Years').val(item.Years ?? '');
        $('#m-Remark').val(item.Remark ?? '');
        await loadPlantOptions(item.LicenseType);
        $('#m-Plant').val(item.Plant ?? '');
        const resp = await fetch(`${EMPLOYEE_API}/${encodeURIComponent(item.EmployeeId)}`);
        if (resp.ok) {
            selectedEmployee = await resp.json();
            $('#m-EmployeeId-display').val(
                selectedEmployee.Name
                    ? `${item.EmployeeId} - ${selectedEmployee.Name}`
                    : item.EmployeeId
            ).prop('disabled', true);
        } else {
            selectedEmployee = null;
            $('#m-EmployeeId-display').val(item.EmployeeId).prop('disabled', true);
        }
        $('#btn-pick-employee').prop('disabled', true);
        updateEmployeeHint();
    }
    $('#header-form').data('mode', mode);
    updateCustomNameVisibility();
    headerModal.show();
}

function updateEmployeeHint() {
    const hint = selectedEmployee?.Department || '';
    $('#m-EmployeeId-hint').text(hint || ' ');
}

function updateLicenseSnapshotOnChange() {
    // 其他:Hours/Years 由 updateCustomNameVisibility 清空並開放手填,不套用主檔快照
    if ($('#m-LicenseType option:selected').data('isOther') === true) return;
    const lic = licenseMap[$('#m-LicenseType').val()];
    $('#m-header-Hours').val(lic && lic.Hours != null ? lic.Hours : '');
    $('#m-header-Years').val(lic && lic.Years != null ? lic.Years : '');
}

function updateCustomNameVisibility() {
    const isOther = $('#m-LicenseType option:selected').data('isOther') === true;
    if (isOther) {
        $('#m-CustomName-group').removeClass('d-none');
        $('#m-Remark-group').addClass('d-none');
        $('#m-CustomName').prop('required', true);
        // 其他:Hours/Years 可手動填,預設清空
        $('#m-header-Hours, #m-header-Years').prop('readonly', false).val('');
    } else {
        $('#m-CustomName-group').addClass('d-none');
        $('#m-Remark-group').removeClass('d-none');
        $('#m-CustomName').prop('required', false);
        // 一般證照:自動帶入、唯讀
        $('#m-header-Hours, #m-header-Years').prop('readonly', true);
    }
}

async function submitHeader(e) {
    e.preventDefault();
    const mode = $('#header-form').data('mode');
    const employeeId = $('#m-EmployeeId').val().trim();
    const licenseType = $('#m-LicenseType').val();
    let remark = $('#m-Remark').val().trim() || null;

    if (mode === 'create') {
        if (!employeeId) {
            showModalError('#header-modal-error', '請選擇員工');
            return;
        }
        if (!licenseType) {
            showModalError('#header-modal-error', '請選擇證照類別');
            return;
        }
    }

    const isOther = mode === 'create' && $('#m-LicenseType option:selected').data('isOther') === true;
    let body;
    if (isOther) {
        const customName = $('#m-CustomName').val().trim();
        if (!customName) {
            showModalError('#header-modal-error', '請輸入自定義證照名稱');
            return;
        }
        const hoursRaw = $('#m-header-Hours').val();
        const yearsRaw = $('#m-header-Years').val();
        if (hoursRaw !== '' && !/^\d+$/.test(hoursRaw)) {
            showModalError('#header-modal-error', '所需時數需為非負整數'); return;
        }
        if (yearsRaw !== '' && !/^\d+$/.test(yearsRaw)) {
            showModalError('#header-modal-error', '年數需為非負整數'); return;
        }
        body = {
            EmployeeId: employeeId,
            LicenseType: licenseType,   // = base 母類碼(99 或 X)
            IsOther: true,
            Remark: customName,
            Plant: $('#m-Plant').val() || null,
            Hours: hoursRaw !== '' ? parseInt(hoursRaw, 10) : null,
            Years: yearsRaw !== '' ? parseInt(yearsRaw, 10) : null
        };
    } else {
        body = { EmployeeId: employeeId, LicenseType: licenseType, Remark: remark, Plant: $('#m-Plant').val() || null };
    }
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
        let savedLicenseType = licenseType;
        try { const dto = await res.json(); if (dto && dto.LicenseType) savedLicenseType = dto.LicenseType; } catch { /* ignore */ }
        await loadHeaders();
        // 新增/修改後回選在該筆表頭上（其他證照用伺服器回傳的 LicenseType，即含序號的代碼如 99.3）
        selectHeaderByKey(employeeId, savedLicenseType);
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
function syncHoursRequired() {
    const isRetrain = $('input[name="m-TrainingType"]:checked').val() === '2';
    $('#m-Hours-required').toggleClass('d-none', !isRetrain);
}

function setTrainingType(type, locked) {
    $(`input[name="m-TrainingType"][value="${type}"]`).prop('checked', true);
    $('input[name="m-TrainingType"]').prop('disabled', locked);
}

function openDetailModal(mode, item) {
    if (!selectedHeader) return;
    $('#detail-modal-error').addClass('d-none').text('');
    $('#detail-modal-title').text(mode === 'create' ? '新增受訓紀錄' : '修改受訓紀錄');

    if (mode === 'create') {
        $('#m-TrainingDate').val('').prop('readonly', false);
        // 首筆鎖定「取得證照」；第二筆起預設「回訓」但可自由切換（2026-08-07 T4）
        setTrainingType(currentDetailCount === 0 ? 1 : 2, currentDetailCount === 0);
        // append-only：新增日期必須晚於最後一筆紀錄；首筆無此限制（後端亦會驗證）
        if (currentDetailCount > 0 && latestDetailDate) {
            $('#m-TrainingDate').attr('min', nextDayIso(latestDetailDate));
        } else {
            $('#m-TrainingDate').removeAttr('min');
        }
        $('#m-Hours').val('');
    } else {
        $('#m-TrainingDate').val(item.TrainingDate).prop('readonly', true).removeAttr('min');
        // 僅最後一筆可修改；單頭僅一筆時該筆即首筆 → 類型鎖定，其餘開放切換
        setTrainingType(item.TrainingType, currentDetailCount === 1);
        $('#m-Hours').val(item.Hours ?? '');
    }
    syncHoursRequired();
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
    const hoursRaw = $('#m-Hours').val();
    const hours = hoursRaw !== '' ? parseFloat(hoursRaw) : null;

    if (!trainingDate) { showModalError('#detail-modal-error', '請選擇受訓日期'); return; }
    if (trainingType === 2 && (hours === null || Number.isNaN(hours) || hours <= 0)) {
        showModalError('#detail-modal-error', '回訓類型時，時數為必填且必須 > 0'); return;
    }

    // 不可未來日（前端先擋，後端 validator 也會擋）
    if (trainingDate > todayLocalIso()) {
        showModalError('#detail-modal-error', '受訓日期不可為未來日');
        return;
    }

    // append-only：新增日期必須晚於最後一筆紀錄（前端先擋，後端 service 也會擋）
    if (mode === 'create' && latestDetailDate && trainingDate <= latestDetailDate) {
        showModalError('#detail-modal-error', `受訓日期必須晚於最後一筆紀錄（${latestDetailDate}）`);
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
        const { EmployeeId, LicenseType } = selectedHeader;
        // 單身改變後單頭衍生欄位（累計、未達時數）也會變，重新撈當前頁
        await loadHeaders();
        // 回選在該筆表頭，並選回剛新增/修改的單身
        selectHeaderByKey(EmployeeId, LicenseType, trainingDate);
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

function nextDayIso(iso) {
    const d = new Date(iso + 'T00:00:00');
    d.setDate(d.getDate() + 1);
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
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
    await ensureAllLicensesLoaded();

    // 部門：直接呼叫 API 取得員工清單後去重排序（trim 防止含空白的同名部門重複出現）
    const empResp = await fetch(EMPLOYEE_API);
    const employees = empResp.ok ? await empResp.json() : [];
    const depts = [...new Set(employees.map(e => (e.Department ?? '').trim()).filter(Boolean))].sort();
    const $dept = $('#adv-Department').empty();
    $('<option></option>').val('').text('（不限）').appendTo($dept);
    depts.forEach(d => $('<option></option>').val(d).text(d).appendTo($dept));

    // 代表廠別：廠別主檔
    const plantResp = await fetch(PLANT_API);
    const plants = plantResp.ok ? await plantResp.json() : [];
    const $plant = $('#adv-Plant').empty();
    $('<option></option>').val('').text('（不限）').appendTo($plant);
    plants.forEach(p => {
        const label = p.PlantName ? `${p.PlantCode} ${p.PlantName}` : p.PlantCode;
        $('<option></option>').val(p.PlantCode).text(label).appendTo($plant);
    });

    // 證照：大類 optgroup（大類本身可選 = 含其下全部），組內列小類（2026-08-07 T3）
    const $lic = $('#adv-LicenseType').empty();
    $('<option></option>').val('').text('（不限）').appendTo($lic);
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    const covered = new Set();
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}（全部）`).appendTo($grp);
        covered.add(cat.LicenseType);
        cachedAllLicenses.filter(x => x.Category === cat.LicenseType).forEach(x => {
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp);
            covered.add(x.LicenseType);
        });
        $grp.appendTo($lic);
    });
    // 未歸入任何大類的小類（防呆）：附掛於清單尾端
    (cachedMinorLicenses || []).filter(x => !covered.has(x.LicenseType)).forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($lic);
    });
}

function updateSortIndicators() {
    $('#training-table thead th.th-sortable').each(function () {
        const active = $(this).data('sort') === sortBy;
        $(this).find('.sort-indicator').text(active ? (sortDesc ? '▼' : '▲') : '');
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

    // 表頭排序：同欄再點切換升/降冪，換欄回到升冪
    $('#training-table thead th.th-sortable').on('click', function () {
        const col = $(this).data('sort');
        if (sortBy === col) {
            sortDesc = !sortDesc;
        } else {
            sortBy = col;
            sortDesc = false;
        }
        updateSortIndicators();
        currentPage = 1;
        loadHeaders();
    });

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
    $('input[name="m-TrainingType"]').on('change', syncHoursRequired);

    updateSortIndicators();   // 顯示預設排序（證照 ▲）

    let _empSearchTimer = null;
    $('#btn-pick-employee').on('click', function () {
        $('#emp-picker-filter').val('');
        renderEmployeePicker([]);
        new bootstrap.Modal(document.getElementById('employee-picker-modal')).show();
    });
    $('#emp-picker-filter').on('input', function () {
        const q = $(this).val().trim();
        clearTimeout(_empSearchTimer);
        if (!q) { renderEmployeePicker([]); return; }
        _empSearchTimer = setTimeout(async () => {
            renderEmployeePicker(await searchEmployees(q));
        }, 300);
    });

    $('#m-LicenseType').on('change', async function () {
        updateCustomNameVisibility();
        updateLicenseSnapshotOnChange();
        await loadPlantOptions($(this).val());
    });

    loadHeaders();
});
