using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IPlantRepository
{
    Task<List<Plant>> GetAllAsync(CancellationToken ct = default);
    Task<Plant?> GetByCodeAsync(string plantCode, CancellationToken ct = default);
}
