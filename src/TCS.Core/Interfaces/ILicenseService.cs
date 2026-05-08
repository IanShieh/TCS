using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Interfaces;

public interface ILicenseService
{
    Task<PagedResult<LicenseMasterDto>> GetAllAsync(int page, int pageSize, string? search = null, LicenseSearchQuery? query = null, CancellationToken ct = default);
    Task<LicenseMasterDto> GetByIdAsync(string licenseType, CancellationToken ct = default);
    Task<LicenseMasterDto> CreateAsync(CreateLicenseMasterRequest req, CancellationToken ct = default);
    Task<LicenseMasterDto> UpdateAsync(UpdateLicenseMasterRequest req, CancellationToken ct = default);
    Task DeleteAsync(string licenseType, CancellationToken ct = default);

    Task<List<LicensePlantRequirementDto>> GetPlantRequirementsAsync(string licenseType, CancellationToken ct = default);
    Task<LicensePlantRequirementDto> CreatePlantRequirementAsync(CreateLicensePlantRequirementRequest req, CancellationToken ct = default);
    Task<LicensePlantRequirementDto> UpdatePlantRequirementAsync(UpdateLicensePlantRequirementRequest req, CancellationToken ct = default);
    Task DeletePlantRequirementAsync(string licenseType, string plant, CancellationToken ct = default);
}
