using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Helpers;
using TCS.Core.Interfaces;
using TCS.Core.Mapping;

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

        var headers = await _repo.GetHeadersAsync(effEmployeeId, effLicenseType, ct);
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
                filtered = filtered.Where(d => d.Department == query.Department);
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
        if (await _repo.HeaderExistsAsync(req.EmployeeId, req.LicenseType, ct))
            throw new InvalidOperationException($"TrainingHeader ({req.EmployeeId},{req.LicenseType}) already exists.");

        var license = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = req.LicenseType,
            Hours = license.Hours ?? 0,
            Years = license.Years,
            Remark = req.Remark,
            Plant = req.Plant
        };
        await _repo.AddHeaderAsync(header, ct);

        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        return header.ToDto(emp, license, new List<TrainingDetail>(), DateOnly.FromDateTime(DateTime.Today));
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

        // §8-2: first detail must be type 1 (取得證照)
        if (!header.Details.Any() && req.TrainingType != (int)TrainingType.取得證照)
            throw new InvalidOperationException("第一筆受訓記錄必須為「取得證照」（TrainingType = 1）。");

        // Check for duplicate date
        var trainingDateTime = req.TrainingDate.ToDateTime(TimeOnly.MinValue);
        if (header.Details.Any(d => d.TrainingDate == trainingDateTime))
            throw new InvalidOperationException($"該受訓日期 {req.TrainingDate:yyyy-MM-dd} 已存在。");

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
        detail.TrainingType = req.TrainingType;
        detail.Hours = req.Hours;
        await _repo.UpdateDetailAsync(detail, ct);
        return detail.ToDto();
    }

    public Task DeleteDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default) =>
        _repo.DeleteDetailAsync(employeeId, licenseType, trainingDate, ct);
}
