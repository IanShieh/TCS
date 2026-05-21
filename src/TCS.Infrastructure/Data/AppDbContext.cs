using ERP.Auth.Common.Services;
using Microsoft.EntityFrameworkCore;
using TCS.Core.Common;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<LicenseMaster> LicenseMasters => Set<LicenseMaster>();
    public DbSet<LicensePlantRequirement> LicensePlantRequirements => Set<LicensePlantRequirement>();
    public DbSet<TrainingHeader> TrainingHeaders => Set<TrainingHeader>();
    public DbSet<TrainingDetail> TrainingDetails => Set<TrainingDetail>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Plant> Plants => Set<Plant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Today.ToString("yyyyMMdd");
        var user = _currentUserService.UserId;
        if (string.IsNullOrEmpty(user)) user = "SYSTEM";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Creator = user;
                entry.Entity.CreateDate = now;
                entry.Entity.Company = "BERLIN";
                entry.Entity.UsrGroup = _currentUserService.UsrGroup;
                entry.Entity.Flag ??= 1;
            }
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.Modifier = user;
                entry.Entity.ModiDate = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
