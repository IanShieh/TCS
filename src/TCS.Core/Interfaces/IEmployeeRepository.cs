using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(string employeeId, CancellationToken ct = default);
}
