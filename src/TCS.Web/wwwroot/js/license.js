const BASE = window.ERP_BASE || '';
const API = BASE + '/api/licenses';
let currentPage = 1;
const pageSize = 20;

async function loadLicenses() {
    const search = $('#search').val();
    const res = await fetch(`${API}?page=${currentPage}&pageSize=${pageSize}&search=${encodeURIComponent(search || '')}`);
    if (!res.ok) { showError(await res.text()); return; }
    const data = await res.json();
    renderTable(data.Items);
    renderPagination(data.TotalPages, data.CurrentPage);
}

function renderTable(items) {
    const tbody = $('#license-table tbody').empty();
    items.forEach(item => {
        tbody.append(`<tr>
            <td>${item.LicenseType}</td>
            <td>${item.Description}</td>
            <td>${item.Category ?? ''}</td>
            <td>${item.Hours ?? ''}</td>
            <td>${item.Years ?? ''}</td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="editLicense('${item.LicenseType}')">修改</button>
                <button class="btn btn-sm btn-danger" onclick="deleteLicense('${item.LicenseType}')">刪除</button>
            </td>
        </tr>`);
    });
}

function renderPagination(totalPages, page) {
    const ul = $('#pagination').empty();
    for (let i = 1; i <= totalPages; i++) {
        ul.append(`<li class="page-item ${i === page ? 'active' : ''}">
            <a class="page-link" href="#" onclick="gotoPage(${i}); return false;">${i}</a>
        </li>`);
    }
}

function gotoPage(page) { currentPage = page; loadLicenses(); }

async function deleteLicense(licenseType) {
    if (!confirm(`確認刪除 ${licenseType}？`)) return;
    const res = await fetch(`${API}/${encodeURIComponent(licenseType)}`, { method: 'DELETE' });
    if (res.ok) loadLicenses();
    else { const err = await res.json(); alert(err.message); }
}

function editLicense(licenseType) {
    window.location.href = `${BASE}/License/Edit/${encodeURIComponent(licenseType)}`;
}

function showError(msg) { console.error(msg); }

$(function () {
    $('#btn-search').on('click', () => { currentPage = 1; loadLicenses(); });
    $('#btn-add').on('click', () => { window.location.href = `${BASE}/License/Create`; });
    loadLicenses();
});
