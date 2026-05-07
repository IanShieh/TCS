using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;
    public EmployeeRepository(AppDbContext db) => _db = db;

    public Task<List<Employee>> GetAllAsync(CancellationToken ct = default) =>
        _db.Employees.ToListAsync(ct);

    public Task<Employee?> GetByIdAsync(string employeeId, CancellationToken ct = default) =>
        _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);
}
