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

public class TrainingRepositoryQueryTests
{
    // 用 SQLite in-memory（關聯式 provider）才能重現 SqlServer 對「必填關聯」Include 產生 INNER JOIN 的行為。
    // 注意：EF InMemory provider 不做關聯式 join 過濾，無法守住此回歸，故這裡不用它。
    private static (AppDbContext db, SqliteConnection conn) BuildContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        // 部署時已移除 TA002→MA001 的實體 FK（見 docs/db-changes-2026-06-14.md），
        // 其他證照才得以寫入無對應主檔的代碼；測試需同樣關閉 FK 強制以反映正式狀態。
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
    public async Task GetHeadersAsync_KeepsOtherLicenseRows_EvenWhenMasterHasNoMatch()
    {
        // 其他證照(99.x / X.0.x)在 TCSMA 無對應列。GetHeadersAsync 必須 LEFT JOIN，
        // 否則(改回 Include→INNER JOIN)這些列會被濾除。本測試實際呼叫 GetHeadersAsync 守住回歸。
        var (db, conn) = BuildContext();
        using var _c = conn;
        using var _d = db;

        // 一般證照：主檔有對應列
        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "1.1", Description = "一般小類" });
        db.TrainingHeaders.Add(new TrainingHeader { EmployeeId = "E001", LicenseType = "1.1" });
        // 其他證照：主檔「無」對應列（會被 INNER JOIN 濾掉的情境）
        db.TrainingHeaders.Add(new TrainingHeader { EmployeeId = "E001", LicenseType = "99.1" });
        await db.SaveChangesAsync();

        var repo = new TrainingRepository(db);
        var headers = await repo.GetHeadersAsync("E001", null);

        // 兩列都要保留；其他證照列若被濾除，這裡會少一列 → 測試失敗
        headers.Should().HaveCount(2);
        headers.Select(h => h.LicenseType).Should().Contain(new[] { "1.1", "99.1" });

        // 主檔無對應 → 導覽屬性為 null（不報錯）
        headers.Single(h => h.LicenseType == "99.1").LicenseMasterNav.Should().BeNull();

        // 主檔有對應 → 正確帶入（LEFT JOIN 仍要能補回主檔）
        var normal = headers.Single(h => h.LicenseType == "1.1");
        normal.LicenseMasterNav.Should().NotBeNull();
        normal.LicenseMasterNav!.Description.Should().Be("一般小類");
    }
}
