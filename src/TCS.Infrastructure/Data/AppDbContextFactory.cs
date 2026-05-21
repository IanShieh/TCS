using ERP.Auth.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TCS.Infrastructure.Data;

/// <summary>供 EF CLI migration 使用的 design-time factory</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=TCS;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }
}

/// <summary>EF design-time 用的 no-op ICurrentUserService，不依賴 HTTP context</summary>
file sealed class DesignTimeCurrentUserService : ICurrentUserService
{
    public string UserId => "SYSTEM";
    public string UserName => "SYSTEM";
    public IEnumerable<string> Roles => Enumerable.Empty<string>();
    public bool IsAuthenticated => false;
    public string UsrGroup => string.Empty;
}
