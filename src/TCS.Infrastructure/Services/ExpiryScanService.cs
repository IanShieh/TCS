using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Services;

public class ExpiryScanService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppTimeProvider _clock;
    private readonly ILogger<ExpiryScanService> _logger;

    public ExpiryScanService(
        IServiceScopeFactory scopeFactory,
        IAppTimeProvider clock,
        ILogger<ExpiryScanService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = TimeZoneInfo.ConvertTime(_clock.UtcNow, _clock.LocalTimeZone);
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now.DateTime;
            _logger.LogInformation("ExpiryScanService: 下次掃描在 {Delay} 後", delay);
            await Task.Delay(delay, stoppingToken);
            await RunScanAsync(stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expiry = scope.ServiceProvider.GetRequiredService<IExpiryCalculator>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var headers = await db.TrainingHeaders
            .Include(h => h.Details)
            .Include(h => h.LicenseMasterNav)
            .ToListAsync(ct);

        foreach (var header in headers)
        {
            if (header.LicenseMasterNav?.Years == null || header.LicenseMasterNav?.Hours == null)
                continue;

            var results = expiry.Calculate(
                header.Details.ToList(),
                header.LicenseMasterNav.Years.Value,
                header.LicenseMasterNav.Hours.Value,
                today);

            var resultMap = results.ToDictionary(r => r.TrainingDate, r => r.IsExpired);
            foreach (var detail in header.Details)
            {
                if (resultMap.TryGetValue(detail.TrainingDate, out var shouldExpire))
                    detail.IsExpired = shouldExpire;
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("ExpiryScanService: 掃描完成 at {Time}", DateTime.Now);
    }
}
