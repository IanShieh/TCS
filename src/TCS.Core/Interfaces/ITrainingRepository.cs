using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface ITrainingRepository
{
    Task<List<TrainingHeader>> GetHeadersAsync(string? employeeId, string? licenseType, CancellationToken ct = default);
    Task<TrainingHeader?> GetHeaderAsync(string employeeId, string licenseType, bool includeDetails = false, CancellationToken ct = default);
    Task<bool> HeaderExistsAsync(string employeeId, string licenseType, CancellationToken ct = default);
    Task AddHeaderAsync(TrainingHeader entity, CancellationToken ct = default);
    Task UpdateHeaderAsync(TrainingHeader entity, CancellationToken ct = default);
    Task DeleteHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default);

    Task<List<TrainingDetail>> GetDetailsAsync(string employeeId, string licenseType, CancellationToken ct = default);
    Task<TrainingDetail?> GetDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default);
    Task AddDetailAsync(TrainingDetail entity, CancellationToken ct = default);
    Task UpdateDetailAsync(TrainingDetail entity, CancellationToken ct = default);
    Task DeleteDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default);

    /// <summary>For ExpiryScanService: fetch all headers with details and license info.</summary>
    Task<List<TrainingHeader>> GetAllWithDetailsAndLicenseAsync(CancellationToken ct = default);
}
