using ERP.Auth.Common.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TCS.Core.Entities;
using TCS.Infrastructure.Data;
using TCS.Infrastructure.Repositories;
using Xunit;

namespace TCS.Tests.Repositories;

public class LicenseRepositoryTests
{
    private static (AppDbContext db, SqliteConnection conn) BuildContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            pragma.ExecuteNonQuery();
        }
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opts, Mock.Of<ICurrentUserService>());
        db.Database.EnsureCreated();
        return (db, conn);
    }

    [Fact]
    public async Task HasChildLicensesAsync_ReturnsTrue_WhenCategoryHasChildren()
    {
        var (db, conn) = BuildContext();
        using var _c = conn;
        using var _d = db;

        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "1", Description = "電氣大類", Category = null });
        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "1.1", Description = "電氣小類", Category = "1" });
        await db.SaveChangesAsync();

        var repo = new LicenseRepository(db);
        (await repo.HasChildLicensesAsync("1")).Should().BeTrue();
    }

    [Fact]
    public async Task HasChildLicensesAsync_ReturnsFalse_WhenCategoryHasNoChildren()
    {
        var (db, conn) = BuildContext();
        using var _c = conn;
        using var _d = db;

        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "5", Description = "獨立真證照", Category = null });
        await db.SaveChangesAsync();

        var repo = new LicenseRepository(db);
        (await repo.HasChildLicensesAsync("5")).Should().BeFalse();
    }
}
