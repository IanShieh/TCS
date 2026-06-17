using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _db;
    public LicenseRepository(AppDbContext db) => _db = db;

    public Task<List<LicenseMaster>> GetAllAsync(CancellationToken ct = default) =>
        _db.LicenseMasters.Include(l => l.PlantRequirements).ToListAsync(ct);

    public Task<LicenseMaster?> GetByIdAsync(string licenseType, CancellationToken ct = default) =>
        _db.LicenseMasters.Include(l => l.PlantRequirements)
            .FirstOrDefaultAsync(l => l.LicenseType == licenseType, ct);

    public Task<bool> ExistsAsync(string licenseType, CancellationToken ct = default) =>
        _db.LicenseMasters.AnyAsync(l => l.LicenseType == licenseType, ct);

    public async Task AddAsync(LicenseMaster entity, CancellationToken ct = default)
    {
        _db.LicenseMasters.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(LicenseMaster entity, CancellationToken ct = default)
    {
        _db.LicenseMasters.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string licenseType, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(licenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{licenseType}' not found.");
        _db.LicenseMasters.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> HasTrainingHeadersAsync(string licenseType, CancellationToken ct = default) =>
        _db.TrainingHeaders.AnyAsync(h => h.LicenseType == licenseType, ct);

    public Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default) =>
        _db.LicenseMasters.AnyAsync(l => l.Category == licenseType, ct);

    public Task<List<LicensePlantRequirement>> GetPlantRequirementsAsync(string licenseType, CancellationToken ct = default) =>
        _db.LicensePlantRequirements.Where(r => r.LicenseType == licenseType).ToListAsync(ct);

    public Task<LicensePlantRequirement?> GetPlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default) =>
        _db.LicensePlantRequirements
            .FirstOrDefaultAsync(r => r.LicenseType == licenseType && r.Plant == plant, ct);

    public async Task AddPlantRequirementAsync(LicensePlantRequirement entity, CancellationToken ct = default)
    {
        _db.LicensePlantRequirements.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePlantRequirementAsync(LicensePlantRequirement entity, CancellationToken ct = default)
    {
        _db.LicensePlantRequirements.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeletePlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default)
    {
        var entity = await GetPlantRequirementAsync(licenseType, plant, ct)
            ?? throw new KeyNotFoundException($"LicensePlantRequirement '{licenseType}/{plant}' not found.");
        _db.LicensePlantRequirements.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
