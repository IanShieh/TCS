using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class TrainingRepository : ITrainingRepository
{
    private readonly AppDbContext _db;
    public TrainingRepository(AppDbContext db) => _db = db;

    public async Task<List<TrainingHeader>> GetHeadersAsync(string? employeeId, string? licenseType, CancellationToken ct = default)
    {
        var q = _db.TrainingHeaders.AsQueryable();
        if (!string.IsNullOrEmpty(employeeId)) q = q.Where(h => h.EmployeeId == employeeId);
        if (!string.IsNullOrEmpty(licenseType)) q = q.Where(h => h.LicenseType == licenseType);

        // LEFT JOIN 證照主檔：其他證照(99.x / X.0.x)在主檔無對應列，需保留該列且 LicenseMasterNav 為 null
        var rows = await q
            .GroupJoin(_db.LicenseMasters, h => h.LicenseType, m => m.LicenseType, (h, ms) => new { h, ms })
            .SelectMany(x => x.ms.DefaultIfEmpty(), (x, m) => new { x.h, m })
            .ToListAsync(ct);

        foreach (var r in rows) r.h.LicenseMasterNav = r.m;
        return rows.Select(r => r.h).ToList();
    }

    public async Task<TrainingHeader?> GetHeaderAsync(string employeeId, string licenseType, bool includeDetails = false, CancellationToken ct = default)
    {
        var q = _db.TrainingHeaders.AsQueryable();
        if (includeDetails) q = q.Include(h => h.Details);
        var header = await q.FirstOrDefaultAsync(h => h.EmployeeId == employeeId && h.LicenseType == licenseType, ct);
        if (header is not null)
            header.LicenseMasterNav = await _db.LicenseMasters.FirstOrDefaultAsync(m => m.LicenseType == licenseType, ct);
        return header;
    }

    public Task<bool> HeaderExistsAsync(string employeeId, string licenseType, CancellationToken ct = default) =>
        _db.TrainingHeaders.AnyAsync(h => h.EmployeeId == employeeId && h.LicenseType == licenseType, ct);

    public Task<List<string>> GetHeaderLicenseTypesByPrefixAsync(string employeeId, string prefix, CancellationToken ct = default) =>
        _db.TrainingHeaders
            .Where(h => h.EmployeeId == employeeId && h.LicenseType.StartsWith(prefix + "."))
            .Select(h => h.LicenseType)
            .ToListAsync(ct);

    public async Task AddHeaderAsync(TrainingHeader entity, CancellationToken ct = default)
    {
        _db.TrainingHeaders.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateHeaderAsync(TrainingHeader entity, CancellationToken ct = default)
    {
        _db.TrainingHeaders.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteHeaderAsync(string employeeId, string licenseType, CancellationToken ct = default)
    {
        var entity = await GetHeaderAsync(employeeId, licenseType, includeDetails: false, ct)
            ?? throw new KeyNotFoundException($"TrainingHeader ({employeeId},{licenseType}) not found.");
        _db.TrainingHeaders.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<TrainingDetail>> GetDetailsAsync(string employeeId, string licenseType, CancellationToken ct = default) =>
        _db.TrainingDetails
            .Where(d => d.EmployeeId == employeeId && d.LicenseType == licenseType)
            .OrderBy(d => d.TrainingDate)
            .ToListAsync(ct);

    public Task<TrainingDetail?> GetDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default) =>
        _db.TrainingDetails.FirstOrDefaultAsync(
            d => d.EmployeeId == employeeId && d.LicenseType == licenseType && d.TrainingDate == trainingDate, ct);

    public async Task AddDetailAsync(TrainingDetail entity, CancellationToken ct = default)
    {
        _db.TrainingDetails.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateDetailAsync(TrainingDetail entity, CancellationToken ct = default)
    {
        _db.TrainingDetails.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteDetailAsync(string employeeId, string licenseType, DateTime trainingDate, CancellationToken ct = default)
    {
        var entity = await GetDetailAsync(employeeId, licenseType, trainingDate, ct)
            ?? throw new KeyNotFoundException($"TrainingDetail ({employeeId},{licenseType},{trainingDate:yyyy-MM-dd}) not found.");
        _db.TrainingDetails.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

}
