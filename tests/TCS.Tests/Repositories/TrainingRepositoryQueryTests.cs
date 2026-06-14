using ERP.Auth.Common.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TCS.Infrastructure.Data;
using Xunit;

namespace TCS.Tests.Repositories;

public class TrainingRepositoryQueryTests
{
    private static AppDbContext BuildContext()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=x;Database=y;Trusted_Connection=True;")
            .Options;
        return new AppDbContext(opts, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void HeadersQuery_UsesLeftJoinToLicenseMaster_SoOtherRowsAreKept()
    {
        // 其他證照(99.x)在 TCSMA 無對應列；headers 查詢必須 LEFT JOIN 才不會把這些列濾掉
        using var db = BuildContext();
        var sql = db.TrainingHeaders
            .GroupJoin(db.LicenseMasters, h => h.LicenseType, m => m.LicenseType, (h, ms) => new { h, ms })
            .SelectMany(x => x.ms.DefaultIfEmpty(), (x, m) => new { x.h, m })
            .ToQueryString();

        sql.Should().Contain("LEFT JOIN");
        sql.Should().NotContain("INNER JOIN");
    }
}
