using DingxinErp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DingxinErp.Infrastructure.Data;

/// <summary>
/// 鼎新 ERP 資料庫 DbContext
/// ★ 注意：鼎新 ERP 使用 char() 欄位，必須搭配 IsFixedLength() 設定
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>是否使用 InMemory 資料庫（影響分頁策略）</summary>
    public bool IsInMemoryDatabase => Database.ProviderName?.Contains("InMemory") == true;

    // ===== 範例表格 (實際使用時依作業需求增減) =====
    public DbSet<SampleHeader> SampleHeaders { get; set; } = null!;
    public DbSet<SampleDetail> SampleDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 套用所有 IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
