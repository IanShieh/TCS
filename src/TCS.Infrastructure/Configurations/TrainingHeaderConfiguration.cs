using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class TrainingHeaderConfiguration : IEntityTypeConfiguration<TrainingHeader>
{
    public void Configure(EntityTypeBuilder<TrainingHeader> builder)
    {
        builder.ToTable("TCSTA");
        builder.HasKey(e => new { e.EmployeeId, e.LicenseType });

        builder.Property(e => e.EmployeeId).HasColumnName("TA001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.LicenseType).HasColumnName("TA002").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnName("TA003").HasMaxLength(6).IsFixedLength(true).IsUnicode(false).IsRequired(false);
        builder.Property(e => e.Hours).HasColumnName("TA004").HasColumnType("int");
        builder.Property(e => e.Remark).HasColumnName("TA005").HasMaxLength(200);
        builder.Property(e => e.Years).HasColumnName("TA006").HasColumnType("int").IsRequired(false);
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);

        builder.HasIndex(e => e.LicenseType).HasDatabaseName("IX_TCSTA_LicenseType");

        builder.HasOne(e => e.LicenseMasterNav)
            .WithMany(m => m.TrainingHeaders)
            .HasForeignKey(e => e.LicenseType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Details)
            .WithOne()
            .HasForeignKey(d => new { d.EmployeeId, d.LicenseType })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
