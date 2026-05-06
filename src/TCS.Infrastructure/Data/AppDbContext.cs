using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
}
