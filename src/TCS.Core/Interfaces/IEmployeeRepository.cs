using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(string employeeId, CancellationToken ct = default);
    Task<List<Employee>> SearchAsync(string q, int maxRows = 50, CancellationToken ct = default);
}
