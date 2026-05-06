using DingxinErp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DingxinErp.Infrastructure.Configurations;

/// <summary>
/// 範例單身 Entity Configuration
/// </summary>
public class SampleDetailConfiguration : IEntityTypeConfiguration<SampleDetail>
{
    public void Configure(EntityTypeBuilder<SampleDetail> builder)
    {
        builder.ToTable("SAMPLE_DETAIL");

        // ===== 複合主鍵 =====
        builder.HasKey(e => new { e.TB001, e.TB002, e.TB003 });

        // ===== 主鍵/外鍵欄位 =====
        builder.Property(e => e.TB001)
            .HasColumnName("TB001")
            .HasMaxLength(4)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TB002)
            .HasColumnName("TB002")
            .HasMaxLength(11)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TB003)
            .HasColumnName("TB003")
            .HasMaxLength(4)
            .IsFixedLength()
            .IsUnicode(false);

        // ===== 業務欄位 =====
        builder.Property(e => e.TB004)
            .HasColumnName("TB004")
            .HasMaxLength(20)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(e => e.TB005)
            .HasColumnName("TB005")
            .HasMaxLength(50);

        builder.Property(e => e.TB006)
            .HasColumnName("TB006")
            .HasColumnType("decimal(16,4)");

        builder.Property(e => e.TB007)
            .HasColumnName("TB007")
            .HasColumnType("decimal(16,4)");

        builder.Property(e => e.TB008)
            .HasColumnName("TB008")
            .HasColumnType("decimal(16,4)");

        builder.Property(e => e.TB009)
            .HasColumnName("TB009")
            .HasMaxLength(255);
    }
}
