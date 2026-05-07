const API = '/api/training-headers';
let currentPage = 1;
const pageSize = 20;

async function loadTrainings() {
    const empId = $('#filter-emp').val();
    const lt = $('#filter-license').val();
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (empId) params.set('employeeId', empId);
    if (lt) params.set('licenseType', lt);
    const res = await fetch(`${API}?${params}`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (!res.ok) { console.error(await res.text()); return; }
    const data = await res.json();
    renderTable(data.Items);
    renderPagination(data.TotalPages, data.CurrentPage);
}

function renderTable(items) {
    const tbody = $('#training-table tbody').empty();
    items.forEach(r => {
        tbody.append(`<tr>
            <td>${r.EmployeeId}</td>
            <td>${r.EmployeeName ?? ''}</td>
            <td>${r.LicenseType}</td>
            <td>${r.Description ?? ''}</td>
            <td>${r.RequiredHours}</td>
            <td>${r.AccumulatedHours}</td>
            <td>${r.RemainingHours}</td>
            <td><span class="badge ${statusClass(r.OverallStatus)}">${statusLabel(r.OverallStatus)}</span></td>
            <td>
                <button class="btn btn-sm btn-info" onclick="viewDetails('${r.EmployeeId}','${r.LicenseType}')">詳細</button>
                <button class="btn btn-sm btn-danger" onclick="deleteHeader('${r.EmployeeId}','${r.LicenseType}')">刪除</button>
            </td>
        </tr>`);
    });
}

function statusClass(s) {
    const map = { 1: 'bg-success', 2: 'bg-warning text-dark', 3: 'bg-danger', 0: 'bg-secondary' };
    return map[s] ?? 'bg-secondary';
}

function statusLabel(s) {
    const map = { 1: '通過', 2: '進行中', 3: '已過期', 0: '未取得' };
    return map[s] ?? '未知';
}

function renderPagination(totalPages, page) {
    const ul = $('#pagination').empty();
    for (let i = 1; i <= totalPages; i++) {
        ul.append(`<li class="page-item ${i === page ? 'active' : ''}">
            <a class="page-link" href="#" onclick="gotoPage(${i}); return false;">${i}</a>
        </li>`);
    }
}

function gotoPage(p) { currentPage = p; loadTrainings(); }

async function deleteHeader(employeeId, licenseType) {
    if (!confirm(`確認刪除 ${employeeId}/${licenseType} 的受訓紀錄？`)) return;
    const res = await fetch(`${API}/${encodeURIComponent(employeeId)}/${encodeURIComponent(licenseType)}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (res.ok) loadTrainings();
    else { const err = await res.json(); alert(err.message); }
}

function viewDetails(employeeId, licenseType) {
    window.location.href = `/TrainingHeader/Details/${encodeURIComponent(employeeId)}/${encodeURIComponent(licenseType)}`;
}

function getToken() { return localStorage.getItem('jwt_token') || ''; }

async function exportExcel() {
    const empId = $('#filter-emp').val();
    const lt = $('#filter-license').val();
    const params = new URLSearchParams();
    if (empId) params.set('employeeId', empId);
    if (lt) params.set('licenseType', lt);
    window.location.href = `/api/export/training-headers?${params}`;
}

$(function () {
    $('#btn-search').on('click', () => { currentPage = 1; loadTrainings(); });
    $('#btn-export').on('click', exportExcel);
    loadTrainings();
});
