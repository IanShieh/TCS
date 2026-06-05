using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Helpers;
using TCS.Core.Interfaces;
using TCS.Core.Mapping;

namespace TCS.Core.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repo;
    private readonly IPlantRepository _plantRepo;

    public LicenseService(ILicenseRepository repo, IPlantRepository plantRepo)
    {
        _repo = repo;
        _plantRepo = plantRepo;
    }

    public async Task<PagedResult<LicenseMasterDto>> GetAllAsync(int page, int pageSize, string? search = null, LicenseSearchQuery? query = null, CancellationToken ct = default)
    {
        IEnumerable<LicenseMaster> all = await _repo.GetAllAsync(ct);

        // Spec §7-1：進階搜尋生效時，快速搜尋自動失效；前端會清空 search，後端再保險過濾。
        var advancedActive = query is { IsEmpty: false };
        if (advancedActive)
        {
            if (!string.IsNullOrWhiteSpace(query!.LicenseTypePrefix))
                all = all.Where(l => l.LicenseType.StartsWith(query.LicenseTypePrefix, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.DescriptionContains))
                all = all.Where(l => l.Description.Contains(query.DescriptionContains, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.Category))
                all = all.Where(l => l.Category == query.Category);
            if (query.HoursMin is not null) all = all.Where(l => l.Hours.HasValue && l.Hours.Value >= query.HoursMin);
            if (query.HoursMax is not null) all = all.Where(l => l.Hours.HasValue && l.Hours.Value <= query.HoursMax);
            if (query.YearsMin is not null) all = all.Where(l => l.Years.HasValue && l.Years.Value >= query.YearsMin);
            if (query.YearsMax is not null) all = all.Where(l => l.Years.HasValue && l.Years.Value <= query.YearsMax);
        }
        else if (!string.IsNullOrEmpty(search))
        {
            all = all.Where(l => l.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || l.LicenseType.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var dtos = all.Select(l => l.ToDto())
                      .OrderBy(NaturalSortKey)
                      .ToList();
        return PaginationHelper.Paginate(dtos, page, pageSize, advancedActive ? null : search);
    }

    public async Task<LicenseMasterDto> GetByIdAsync(string licenseType, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(licenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{licenseType}' not found.");
        return entity.ToDto();
    }

    public async Task<LicenseMasterDto> CreateAsync(CreateLicenseMasterRequest req, CancellationToken ct = default)
    {
        if (await _repo.ExistsAsync(req.LicenseType, ct))
            throw new InvalidOperationException($"LicenseType '{req.LicenseType}' already exists.");
        var entity = new LicenseMaster
        {
            LicenseType = req.LicenseType,
            Description = req.Description,
            Category = req.Category,
            Hours = req.Hours,
            Years = req.Years
        };
        await _repo.AddAsync(entity, ct);
        return entity.ToDto();
    }

    public async Task<LicenseMasterDto> UpdateAsync(UpdateLicenseMasterRequest req, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");
        entity.Description = req.Description;
        entity.Category = req.Category;
        entity.Hours = req.Hours;
        entity.Years = req.Years;
        await _repo.UpdateAsync(entity, ct);
        return entity.ToDto();
    }

    public async Task DeleteAsync(string licenseType, CancellationToken ct = default)
    {
        if (await _repo.HasTrainingHeadersAsync(licenseType, ct))
            throw new InvalidOperationException("Cannot delete: training records exist for this license type.");
        await _repo.DeleteAsync(licenseType, ct);
    }

    public async Task<List<LicensePlantRequirementDto>> GetPlantRequirementsAsync(string licenseType, CancellationToken ct = default)
    {
        var reqs = await _repo.GetPlantRequirementsAsync(licenseType, ct);
        var plants = await _plantRepo.GetAllAsync(ct);
        var plantMap = plants.ToDictionary(p => p.PlantCode.Trim(), p => p.PlantName);
        return reqs.Select(r => r.ToDto(plantMap.GetValueOrDefault(r.Plant.Trim()))).ToList();
    }

    public async Task<LicensePlantRequirementDto> CreatePlantRequirementAsync(CreateLicensePlantRequirementRequest req, CancellationToken ct = default)
    {
        var existing = await _repo.GetPlantRequirementAsync(req.LicenseType, req.Plant, ct);
        if (existing != null)
            throw new InvalidOperationException($"Plant requirement ({req.LicenseType}/{req.Plant}) already exists.");
        var entity = new LicensePlantRequirement
        {
            LicenseType = req.LicenseType,
            Plant = req.Plant,
            RequiredCount = req.RequiredCount
        };
        await _repo.AddPlantRequirementAsync(entity, ct);
        var plant = await _plantRepo.GetByCodeAsync(req.Plant, ct);
        return entity.ToDto(plant?.PlantName);
    }

    public async Task<LicensePlantRequirementDto> UpdatePlantRequirementAsync(UpdateLicensePlantRequirementRequest req, CancellationToken ct = default)
    {
        var entity = await _repo.GetPlantRequirementAsync(req.LicenseType, req.Plant, ct)
            ?? throw new KeyNotFoundException($"Plant requirement ({req.LicenseType}/{req.Plant}) not found.");
        entity.RequiredCount = req.RequiredCount;
        await _repo.UpdatePlantRequirementAsync(entity, ct);
        var plant = await _plantRepo.GetByCodeAsync(req.Plant, ct);
        return entity.ToDto(plant?.PlantName);
    }

    public Task DeletePlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default) =>
        _repo.DeletePlantRequirementAsync(licenseType, plant, ct);

    // 將 "1.10" → "000001.000010" 使字串排序等同自然數排序
    private static string NaturalSortKey(LicenseMasterDto dto) =>
        string.Join(".", dto.LicenseType.Split('.')
            .Select(seg => int.TryParse(seg, out var n) ? n.ToString("D6") : seg));
}
