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

        builder.Property(e => e.LicenseType).HasColumnName("MA001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Description).HasColumnName("MA002").HasMaxLength(100);
        builder.Property(e => e.Category).HasColumnName("MA003").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Hours).HasColumnName("MA004");
        builder.Property(e => e.Years).HasColumnName("MA005");
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
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
