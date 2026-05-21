using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicenseMasterConfiguration : IEntityTypeConfiguration<LicenseMaster>
{
    public void Configure(EntityTypeBuilder<LicenseMaster> builder)
    {
        builder.ToTable("TCSMA");
        builder.HasKey(e => e.LicenseType);
        builder.Property(e => e.LicenseType).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Description).HasMaxLength(100);
        builder.Property(e => e.Category).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Hours);
        builder.Property(e => e.Years);
        builder.Property(e => e.Creator).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnType("decimal(1,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);

        builder.HasMany(e => e.PlantRequirements)
            .WithOne(r => r.LicenseMasterNav)
            .HasForeignKey(r => r.LicenseType)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TrainingHeaders)
            .WithOne(h => h.LicenseMasterNav)
            .HasForeignKey(h => h.LicenseType)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
