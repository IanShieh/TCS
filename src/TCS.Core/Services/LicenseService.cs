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

    public async Task<PagedResult<LicenseMasterDto>> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        if (!string.IsNullOrEmpty(search))
            all = all.Where(l => l.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || l.LicenseType.Contains(search, StringComparison.OrdinalIgnoreCase))
                     .ToList();
        var dtos = all.Select(l => l.ToDto()).ToList();
        return PaginationHelper.Paginate(dtos, page, pageSize, search);
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
        var plantMap = plants.ToDictionary(p => p.PlantCode, p => p.PlantName);
        return reqs.Select(r => r.ToDto(plantMap.GetValueOrDefault(r.Plant))).ToList();
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
}
