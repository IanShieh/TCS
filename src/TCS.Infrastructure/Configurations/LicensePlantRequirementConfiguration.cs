using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicensePlantRequirementConfiguration : IEntityTypeConfiguration<LicensePlantRequirement>
{
    public void Configure(EntityTypeBuilder<LicensePlantRequirement> builder)
    {
        builder.ToTable("TCSMB");
        builder.HasKey(e => new { e.LicenseType, e.Plant });

        builder.Property(e => e.LicenseType).HasColumnName("MB001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnName("MB002").HasMaxLength(10).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.RequiredCount).HasColumnName("MB003");
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);
    }
}
