using DingxinErp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DingxinErp.Infrastructure.Configurations;

/// <summary>
/// 範例單頭 Entity Configuration
/// ★ 鼎新 ERP 的 char() 欄位必須設定 IsFixedLength()，否則 EF Core 查詢會加 N'' 前綴導致查不到
/// </summary>
public class SampleHeaderConfiguration : IEntityTypeConfiguration<SampleHeader>
{
    public void Configure(EntityTypeBuilder<SampleHeader> builder)
    {
        builder.ToTable("SAMPLE_HEADER");

        // ===== 複合主鍵 =====
        builder.HasKey(e => new { e.TA001, e.TA002 });

        // ===== 主鍵欄位 (char = IsFixedLength + IsUnicode(false)) =====
        builder.Property(e => e.TA001)
            .HasColumnName("TA001")
            .HasMaxLength(4)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TA002)
            .HasColumnName("TA002")
            .HasMaxLength(11)
            .IsFixedLength()
            .IsUnicode(false);

        // ===== 業務欄位 =====
        builder.Property(e => e.TA003)
            .HasColumnName("TA003")
            .HasMaxLength(8)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TA004)
            .HasColumnName("TA004")
            .HasMaxLength(10)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TA005)
            .HasColumnName("TA005")
            .HasMaxLength(30);

        builder.Property(e => e.TA006)
            .HasColumnName("TA006")
            .HasMaxLength(255);

        builder.Property(e => e.TA007)
            .HasColumnName("TA007")
            .HasMaxLength(1)
            .IsFixedLength()
            .IsUnicode(false)
            .HasDefaultValue("Y");

        // ===== 審計欄位 =====
        builder.Property(e => e.Creator)
            .HasColumnName("CREATOR")
            .HasMaxLength(10)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.CreateDate)
            .HasColumnName("CREATE_DATE")
            .HasMaxLength(8)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.Modifier)
            .HasColumnName("MODIFIER")
            .HasMaxLength(10)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.ModiDate)
            .HasColumnName("MODI_DATE")
            .HasMaxLength(8)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.Flag)
            .HasColumnName("FLAG")
            .HasColumnType("decimal(1,0)");

        // ===== 單頭→單身 一對多關聯 + Cascade Delete =====
        builder.HasMany(e => e.Details)
            .WithOne(d => d.Header)
            .HasForeignKey(d => new { d.TB001, d.TB002 })
            .HasPrincipalKey(e => new { e.TA001, e.TA002 })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
