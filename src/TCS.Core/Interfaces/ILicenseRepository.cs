using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface ILicenseRepository
{
    Task<List<LicenseMaster>> GetAllAsync(CancellationToken ct = default);
    Task<LicenseMaster?> GetByIdAsync(string licenseType, CancellationToken ct = default);
    Task<bool> ExistsAsync(string licenseType, CancellationToken ct = default);
    Task AddAsync(LicenseMaster entity, CancellationToken ct = default);
    Task UpdateAsync(LicenseMaster entity, CancellationToken ct = default);
    Task DeleteAsync(string licenseType, CancellationToken ct = default);
    Task<bool> HasTrainingHeadersAsync(string licenseType, CancellationToken ct = default);
    Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default);

    Task<List<LicensePlantRequirement>> GetPlantRequirementsAsync(string licenseType, CancellationToken ct = default);
    /// <summary>依廠別反查需求（含證照主檔導覽屬性；廠別需求總覽用）</summary>
    Task<List<LicensePlantRequirement>> GetPlantRequirementsByPlantAsync(string plant, CancellationToken ct = default);
    Task<LicensePlantRequirement?> GetPlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default);
    Task AddPlantRequirementAsync(LicensePlantRequirement entity, CancellationToken ct = default);
    Task UpdatePlantRequirementAsync(LicensePlantRequirement entity, CancellationToken ct = default);
    Task DeletePlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default);
}
