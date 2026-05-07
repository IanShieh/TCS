using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class PlantRepository : IPlantRepository
{
    private readonly AppDbContext _db;
    public PlantRepository(AppDbContext db) => _db = db;

    public Task<List<Plant>> GetAllAsync(CancellationToken ct = default) =>
        _db.Plants.ToListAsync(ct);

    public Task<Plant?> GetByCodeAsync(string plantCode, CancellationToken ct = default) =>
        _db.Plants.FirstOrDefaultAsync(p => p.PlantCode == plantCode, ct);
}
