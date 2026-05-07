using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Interfaces;

public interface ITrainingService
{
    Task<PagedResult<TrainingHeaderDto>> GetHeadersAsync(string? employeeId, string? licenseType, int page, int pageSize, CancellationToken ct = default);
    Task<TrainingHeaderDto> GetHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default);
    Task<TrainingHeaderDto> CreateHeaderAsync(CreateTrainingHeaderRequest req, CancellationToken ct = default);
    Task<TrainingHeaderDto> UpdateHeaderAsync(UpdateTrainingHeaderRequest req, CancellationToken ct = default);
    Task DeleteHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default);

    Task<List<TrainingDetailDto>> GetDetailsAsync(string employeeId, string licenseType, CancellationToken ct = default);
    Task<TrainingDetailDto> AddDetailAsync(CreateTrainingDetailRequest req, CancellationToken ct = default);
    Task<TrainingDetailDto> UpdateDetailAsync(UpdateTrainingDetailRequest req, CancellationToken ct = default);
    Task DeleteDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default);
}
