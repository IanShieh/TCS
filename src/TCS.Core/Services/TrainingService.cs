using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Helpers;
using TCS.Core.Interfaces;
using TCS.Core.Mapping;
using TCS.Core.Validators;

namespace TCS.Core.Services;

public class TrainingService : ITrainingService
{
    private readonly ITrainingRepository _repo;
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _empRepo;

    public TrainingService(
        ITrainingRepository repo,
        ILicenseRepository licenseRepo,
        IEmployeeRepository empRepo)
    {
        _repo = repo;
        _licenseRepo = licenseRepo;
        _empRepo = empRepo;
    }

    public async Task<PagedResult<TrainingHeaderDto>> GetHeadersAsync(
        string? employeeId, string? licenseType, int page, int pageSize, TrainingSearchQuery? query = null, CancellationToken ct = default)
    {
        // 進階搜尋生效時，DB 層的 employeeId/licenseType 改採 query 提供值（spec §7-1：兩段式同時生效以進階為準）
        var advancedActive = query?.IsAdvancedActive == true;
        var effEmployeeId = advancedActive ? query!.EmployeeId : employeeId;
        var effLicenseType = advancedActive ? query!.LicenseType : licenseType;

        // 大類展開（2026-08-07 T3）：選大類 → 含本身 + Category 小類 + 「碼.」前綴其他證照（99.x / X.0.x）
        // 跨 LicenseMaster 判定屬 Service 職責（讀取分層規則）；非大類維持 Repo 完全比對
        string? repoLicenseType = effLicenseType;
        HashSet<string>? majorExpansion = null;
        string? majorPrefix = null;
        if (!string.IsNullOrWhiteSpace(effLicenseType))
        {
            var lic = await _licenseRepo.GetByIdAsync(effLicenseType, ct);
            if (lic is not null && MappingExtensions.IsLicenseTypeCategory(lic.LicenseType))
            {
                repoLicenseType = null;
                majorPrefix = effLicenseType + ".";
                var allLicenses = await _licenseRepo.GetAllAsync(ct);
                majorExpansion = allLicenses
                    .Where(m => m.Category == effLicenseType)
                    .Select(m => m.LicenseType)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                majorExpansion.Add(effLicenseType);
            }
        }

        var headers = await _repo.GetHeadersAsync(effEmployeeId, repoLicenseType, ct);
        if (majorExpansion is not null)
            headers = headers
                .Where(h => majorExpansion.Contains(h.LicenseType)
                            || h.LicenseType.StartsWith(majorPrefix!, StringComparison.Ordinal))
                .ToList();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dtos = new List<TrainingHeaderDto>();
        foreach (var h in headers)
        {
            var details = await _repo.GetDetailsAsync(h.EmployeeId, h.LicenseType, ct);
            var emp = await _empRepo.GetByIdAsync(h.EmployeeId, ct);
            dtos.Add(h.ToDto(emp, h.LicenseMasterNav, details, today));
        }

        IEnumerable<TrainingHeaderDto> filtered = dtos;

        if (advancedActive)
        {
            if (!string.IsNullOrWhiteSpace(query!.NameContains))
                filtered = filtered.Where(d => d.EmployeeName != null && d.EmployeeName.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.Department))
                filtered = filtered.Where(d => d.Department != null && d.Department.Trim() == query.Department.Trim());
            if (!string.IsNullOrWhiteSpace(query.Plant))
                filtered = filtered.Where(d => d.Plant == query.Plant);
            if (query.ExpiredOnly == true)
                filtered = filtered.Where(d => d.OverallStatus == OverallStatus.已過期);
            if (query.UnmetHoursOnly == true)
                filtered = filtered.Where(d => d.RemainingHours > 0);
            if (query.NextReviewFrom is not null)
                filtered = filtered.Where(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value >= query.NextReviewFrom.Value);
            if (query.NextReviewTo is not null)
                filtered = filtered.Where(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value <= query.NextReviewTo.Value);
        }
        else if (!string.IsNullOrWhiteSpace(query?.Search))
        {
            var kw = query.Search!;
            filtered = filtered.Where(d =>
                (d.EmployeeId?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true)
                || (d.EmployeeName?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true)
                || (d.LicenseType?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true)
                || (d.Description?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true));
        }

        // 排序須於分頁前套用。預設證照升冪；主排序切換時另一欄固定為次要排序（升冪）。
        // 證照採自然排序（與證照管理頁一致）；非白名單 SortBy 一律回落預設排序。
        var cmp = StringComparer.OrdinalIgnoreCase;
        if (query?.SortBy == "EmployeeId")
        {
            var primary = query.SortDesc
                ? filtered.OrderByDescending(d => d.EmployeeId, cmp)
                : filtered.OrderBy(d => d.EmployeeId, cmp);
            filtered = primary.ThenBy(d => LicenseTypeSort.NaturalKey(d.LicenseType), cmp);
        }
        else
        {
            var primary = query?.SortBy == "LicenseType" && query.SortDesc
                ? filtered.OrderByDescending(d => LicenseTypeSort.NaturalKey(d.LicenseType), cmp)
                : filtered.OrderBy(d => LicenseTypeSort.NaturalKey(d.LicenseType), cmp);
            filtered = primary.ThenBy(d => d.EmployeeId, cmp);
        }

        return PaginationHelper.Paginate(filtered.ToList(), page, pageSize);
    }

    public async Task<TrainingHeaderDto> GetHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default)
    {
        var header = await _repo.GetHeaderAsync(employeeId, licenseType, includeDetails: true, ct)
            ?? throw new KeyNotFoundException($"TrainingHeader ({employeeId},{licenseType}) not found.");
        var emp = await _empRepo.GetByIdAsync(employeeId, ct);
        return header.ToDto(emp, header.LicenseMasterNav, header.Details.ToList(), DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<TrainingHeaderDto> CreateHeaderAsync(CreateTrainingHeaderRequest req, CancellationToken ct = default)
    {
        if (req.IsOther)
            return await CreateOtherHeaderAsync(req, ct);

        if (await _repo.HeaderExistsAsync(req.EmployeeId, req.LicenseType, ct))
            throw new InvalidOperationException($"TrainingHeader ({req.EmployeeId},{req.LicenseType}) already exists.");

        var license = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

        // 純整數大類:僅「非 99 且底下無小類」可直接作為受訓對象（語意閘，validator 不查 DB）
        if (ValidatorHelpers.IsLicenseTypeCategory(req.LicenseType))
        {
            if (req.LicenseType == "99")
                throw new InvalidOperationException("「其他」大類(99)不可直接選取,請改用其他證照流程。");
            if (await _licenseRepo.HasChildLicensesAsync(req.LicenseType, ct))
                throw new InvalidOperationException($"大類 {req.LicenseType} 底下尚有小類,不可直接作為受訓對象,請選擇小類。");
        }

        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = req.LicenseType,
            Hours = license.Hours,
            Years = license.Years,
            Remark = req.Remark,
            Plant = req.Plant
        };
        await _repo.AddHeaderAsync(header, ct);

        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        return header.ToDto(emp, license, new List<TrainingDetail>(), DateOnly.FromDateTime(DateTime.Today));
    }

    // 其他證照:以 base 母類碼產生每位員工各自的唯一代碼(99.{n} / X.0.{n}),
    // 產生碼只寫入 TCSTA;Hours/Years 由使用者手動填(可空),自定義名稱存 Remark。
    private async Task<TrainingHeaderDto> CreateOtherHeaderAsync(CreateTrainingHeaderRequest req, CancellationToken ct)
    {
        _ = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

        var prefix = OtherLicenseCode.Prefix(req.LicenseType);
        var existing = await _repo.GetHeaderLicenseTypesByPrefixAsync(req.EmployeeId, prefix, ct);
        var newCode = OtherLicenseCode.Next(prefix, existing);

        if (await _repo.HeaderExistsAsync(req.EmployeeId, newCode, ct))
            throw new InvalidOperationException($"TrainingHeader ({req.EmployeeId},{newCode}) already exists.");

        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = newCode,
            Hours = req.Hours,
            Years = req.Years,
            Remark = req.Remark,
            Plant = req.Plant
        };
        await _repo.AddHeaderAsync(header, ct);

        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        // 產生碼無對應主檔,Description 留空(自定義名稱在 Remark 欄顯示)
        return header.ToDto(emp, null, new List<TrainingDetail>(), DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<TrainingHeaderDto> UpdateHeaderAsync(UpdateTrainingHeaderRequest req, CancellationToken ct = default)
    {
        var header = await _repo.GetHeaderAsync(req.EmployeeId, req.LicenseType, includeDetails: true, ct)
            ?? throw new KeyNotFoundException($"TrainingHeader ({req.EmployeeId},{req.LicenseType}) not found.");
        header.Remark = req.Remark;
        header.Plant = req.Plant;
        await _repo.UpdateHeaderAsync(header, ct);
        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        return header.ToDto(emp, header.LicenseMasterNav, header.Details.ToList(), DateOnly.FromDateTime(DateTime.Today));
    }

    public Task DeleteHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default) =>
        _repo.DeleteHeaderAsync(employeeId, licenseType, ct);

    public async Task<List<TrainingDetailDto>> GetDetailsAsync(string employeeId, string licenseType, CancellationToken ct = default)
    {
        var details = await _repo.GetDetailsAsync(employeeId, licenseType, ct);
        return details.Select(d => d.ToDto()).ToList();
    }

    public async Task<TrainingDetailDto> AddDetailAsync(CreateTrainingDetailRequest req, CancellationToken ct = default)
    {
        var header = await _repo.GetHeaderAsync(req.EmployeeId, req.LicenseType, includeDetails: true, ct)
            ?? throw new KeyNotFoundException($"TrainingHeader ({req.EmployeeId},{req.LicenseType}) not found.");

        // 首筆必須是 type 1（取得證照）；第二筆起類型不限（過期重考可再取證，2026-08-07 T4）
        if (!header.Details.Any() && req.TrainingType != (int)TrainingType.取得證照)
            throw new InvalidOperationException("第一筆受訓記錄必須為「取得證照」（TrainingType = 1）。");

        // Check for duplicate date
        var trainingDateTime = req.TrainingDate.ToDateTime(TimeOnly.MinValue);
        if (header.Details.Any(d => d.TrainingDate == trainingDateTime))
            throw new InvalidOperationException($"該受訓日期 {req.TrainingDate:yyyy-MM-dd} 已存在。");

        // append-only: 新增受訓日期必須晚於目前最後一筆紀錄（配合 UI「僅最後一筆可編輯/刪除」）。
        // 同時保證回訓日期必晚於取得證照（anchor），維持 ToDto roll-forward 推導正確。
        if (header.Details.Any())
        {
            var latest = header.Details.Max(d => d.TrainingDate);
            if (trainingDateTime <= latest)
                throw new InvalidOperationException(
                    $"受訓日期必須晚於最後一筆受訓紀錄（{DateOnly.FromDateTime(latest):yyyy-MM-dd}）。");
        }

        var detail = new TrainingDetail
        {
            EmployeeId = req.EmployeeId,
            LicenseType = req.LicenseType,
            TrainingDate = trainingDateTime,
            TrainingType = req.TrainingType,
            Hours = req.Hours
        };
        await _repo.AddDetailAsync(detail, ct);
        return detail.ToDto();
    }

    public async Task<TrainingDetailDto> UpdateDetailAsync(UpdateTrainingDetailRequest req, CancellationToken ct = default)
    {
        var trainingDateTime = req.TrainingDate.ToDateTime(TimeOnly.MinValue);
        var detail = await _repo.GetDetailAsync(req.EmployeeId, req.LicenseType, trainingDateTime, ct)
            ?? throw new KeyNotFoundException($"TrainingDetail ({req.EmployeeId},{req.LicenseType},{req.TrainingDate:yyyy-MM-dd}) not found.");

        // 首筆（最早一筆）只擋「從取得證照改走」；類型不變（含 legacy 資料違反不變式的情形）或改回取得證照皆放行（2026-08-07 T4 回歸修正）
        var details = await _repo.GetDetailsAsync(req.EmployeeId, req.LicenseType, ct);
        var earliest = details.Min(d => d.TrainingDate);
        if (detail.TrainingDate == earliest
            && req.TrainingType != detail.TrainingType
            && req.TrainingType != (int)TrainingType.取得證照)
            throw new InvalidOperationException("首筆受訓記錄類型僅能維持或改回「取得證照」（TrainingType = 1）。");

        detail.TrainingType = req.TrainingType;
        detail.Hours = req.Hours;
        await _repo.UpdateDetailAsync(detail, ct);
        return detail.ToDto();
    }

    public Task DeleteDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default) =>
        _repo.DeleteDetailAsync(employeeId, licenseType, trainingDate, ct);
}
